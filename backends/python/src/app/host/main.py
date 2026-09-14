"""The composition root — the only place that wires a concrete
implementation to every port a feature declared. Nothing here is imported
by any feature; everything here imports features.
"""

from __future__ import annotations

import asyncio
import contextlib
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
from app.shared.jobs.scheduler import InProcessJobScheduler
from app.shared.localization.localizer import JsonAppLocalizer
from app.shared.observability.logging_setup import configure_logging
from app.shared.observability.tracing import configure_opentelemetry, instrument_app
from app.shared.realtime.publisher import ConnectionRegistry, InProcessRealtimePublisher, RedisRealtimePublisher
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

        # Realtime: a ConnectionRegistry always holds this process's own
        # WebSocket connections. A Redis (or Valkey) URL layers a backplane
        # underneath so a message published on one instance reaches a
        # client connected to another; left unset, publishing still
        # delivers to every client connected to *this* process — correct
        # for one instance, and needs nothing running.
        app.state.realtime_registry = ConnectionRegistry()
        background_tasks: list[asyncio.Task] = []
        redis_client = None
        if settings.realtime_redis_url:
            from redis.asyncio import from_url as redis_from_url

            redis_client = redis_from_url(settings.realtime_redis_url)
            realtime_publisher = RedisRealtimePublisher(redis_client, app.state.realtime_registry)
            background_tasks.append(asyncio.create_task(realtime_publisher.subscribe_forever()))
            app.state.realtime_publisher = realtime_publisher
        else:
            app.state.realtime_publisher = InProcessRealtimePublisher(app.state.realtime_registry)

        app.state.job_scheduler = InProcessJobScheduler()
        background_tasks.append(asyncio.create_task(app.state.job_scheduler.run_forever()))

        if settings.run_migrations_on_startup:
            await asyncio.to_thread(_run_migrations, settings.postgres_dsn)

        if settings.seed_on_startup:
            await seed(session_factory, app.state.password_hasher)

        yield

        for task in background_tasks:
            task.cancel()
        for task in background_tasks:
            with contextlib.suppress(asyncio.CancelledError):
                await task
        if redis_client is not None:
            await redis_client.aclose()
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
