"""The composition root — the only place that wires a concrete
implementation to every port a feature declared. Nothing here is imported
by any feature; everything here imports features.
"""

from __future__ import annotations

import asyncio
import subprocess
import sys
from contextlib import asynccontextmanager
from pathlib import Path
from typing import AsyncIterator

from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from starlette.responses import JSONResponse

from app.database.postgres.engine import create_engine, create_session_factory
from app.database.postgres.seed import seed
from app.features.identity.endpoints.security import (
    AuthenticationError,
    ForbiddenError,
    JwtAccessTokenIssuer,
    JwtOptions,
    app_error_for_auth_failure,
    app_error_for_forbidden,
)
from app.features.identity.endpoints.router import router as identity_router
from app.host.config import Settings
from app.shared.localization.localizer import JsonAppLocalizer
from app.shared.observability.logging_setup import configure_logging
from app.shared.observability.tracing import configure_opentelemetry, instrument_app
from app.shared.security.password_hasher import Pbkdf2PasswordHasher
from app.shared.web.correlation import CorrelationIdMiddleware
from app.shared.web.exception_handling import register_exception_handlers
from app.shared.web.problem import problem_response
from app.shared.web.rate_limit import FixedWindowRateLimiter, RateLimitExceededError, rate_limited_response

_ALEMBIC_INI = Path(__file__).resolve().parents[3] / "alembic.ini"


def _run_migrations(dsn: str) -> None:
    # env.py builds its own AsyncEngine (the documented Alembic recipe for an
    # async dialect), so the DSN keeps its "+asyncpg" driver qualifier here —
    # unlike a typical sync Alembic setup, it must not be stripped.
    subprocess.run(
        [sys.executable, "-m", "alembic", "-c", str(_ALEMBIC_INI), "upgrade", "head"],
        check=True,
        env={"STACKBRAID_POSTGRES_DSN": dsn, "PATH": _path_env()},
    )


def _path_env() -> str:
    import os

    return os.environ.get("PATH", "")


def create_app(settings: Settings | None = None) -> FastAPI:
    settings = settings or Settings()
    configure_logging()
    configure_opentelemetry(settings.otel_otlp_endpoint)
    localizer = JsonAppLocalizer()

    @asynccontextmanager
    async def lifespan(app: FastAPI) -> AsyncIterator[None]:
        engine = create_engine(settings.postgres_dsn)
        session_factory = create_session_factory(engine)

        app.state.session_factory = session_factory
        app.state.localizer = localizer
        app.state.password_hasher = Pbkdf2PasswordHasher()
        app.state.jwt_options = JwtOptions(
            signing_key=settings.jwt_signing_key,
            issuer=settings.jwt_issuer,
            audience=settings.jwt_audience,
            access_token_lifetime_seconds=settings.jwt_access_token_lifetime_seconds,
        )
        app.state.access_token_issuer = JwtAccessTokenIssuer(app.state.jwt_options)
        app.state.auth_rate_limiter = FixedWindowRateLimiter(permit_limit=settings.auth_rate_limit_permits_per_minute)

        if settings.run_migrations_on_startup:
            await asyncio.to_thread(_run_migrations, settings.postgres_dsn)

        if settings.seed_on_startup:
            await seed(session_factory, app.state.password_hasher)

        yield

        await engine.dispose()

    app = FastAPI(title="StackBraid Identity API", version="1.0.0", lifespan=lifespan)
    instrument_app(app)

    # No frontend origin is trusted by default — a frontend must be listed
    # explicitly (STACKBRAID_CORS_ALLOWED_ORIGINS_RAW, e.g. a frontend's
    # local dev server) before its browser requests are allowed to carry
    # the httpOnly refresh cookie.
    if settings.cors_allowed_origins:
        app.add_middleware(
            CORSMiddleware,
            allow_origins=settings.cors_allowed_origins,
            allow_credentials=True,
            allow_methods=["*"],
            allow_headers=["*"],
        )

    app.add_middleware(CorrelationIdMiddleware)
    register_exception_handlers(app, localizer)

    @app.exception_handler(AuthenticationError)
    async def handle_authentication_error(request: Request, exc: AuthenticationError) -> JSONResponse:
        return problem_response(app_error_for_auth_failure(), localizer, request)

    @app.exception_handler(ForbiddenError)
    async def handle_forbidden_error(request: Request, exc: ForbiddenError) -> JSONResponse:
        return problem_response(app_error_for_forbidden(), localizer, request)

    @app.exception_handler(RateLimitExceededError)
    async def handle_rate_limit_error(request: Request, exc: RateLimitExceededError) -> JSONResponse:
        return rate_limited_response(request, localizer)

    @app.get("/health/live")
    async def health_live() -> dict:
        return {"status": "live"}

    @app.get("/health/ready")
    async def health_ready() -> dict:
        async with app.state.session_factory() as session:
            from sqlalchemy import text

            await session.execute(text("SELECT 1"))
        return {"status": "ready"}

    app.include_router(identity_router)

    return app


app = create_app()
