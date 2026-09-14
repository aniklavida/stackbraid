"""No secret — a password, a refresh token, an ``Authorization`` header —
may ever reach a log line or an exported span attribute. This drives every
place a secret travels in the real Identity flow (register, login, refresh
via cookie, an authenticated call, logout via bearer token) against the
real running app (real Postgres, real JWT issuance, real OpenTelemetry
instrumentation, real structured logging) and inspects everything the
process actually recorded — not a mock standing in for "what it probably
does". The .NET backend's own
``Features.Identity.IntegrationTests.SecretRedactionTests`` proves the same
thing there; this is the Python-side counterpart.

Two independent capture points, both wired additively (nothing here
replaces the app's own logging or tracing configuration):
 - Every byte written to ``sys.stdout`` while the app is created and the
   request flow runs — where the structured JSON log formatter actually
   writes (see ``shared/observability/logging_setup.py``).
 - A span processor added directly onto whatever ``TracerProvider`` is
   already configured (this process's OpenTelemetry SDK only allows the
   *first* ``set_tracer_provider`` call to take effect, so a redundant
   provider from this test would silently be ignored — adding one more
   processor onto the existing provider works regardless of who created
   it), recording every span's own attributes directly.
"""

from __future__ import annotations

import contextlib
import io
import logging
import sys
import uuid

import httpx
import pytest
from opentelemetry import trace
from opentelemetry.sdk.trace import ReadableSpan, TracerProvider
from opentelemetry.sdk.trace.export import SimpleSpanProcessor, SpanExporter, SpanExportResult

from app.database.postgres.engine import create_engine
from app.features.identity.persistence import models  # noqa: F401 - registers ORM tables on OrmBase.metadata
from app.host.config import Settings
from app.host.main import create_app
from app.shared.persistence.base import OrmBase
from tests.integration.conftest import _dsn_or_skip


async def _ensure_schema(dsn: str) -> None:
    engine = create_engine(dsn)
    async with engine.begin() as conn:
        await conn.run_sync(OrmBase.metadata.create_all)
    await engine.dispose()


class _CapturingSpanExporter(SpanExporter):
    def __init__(self) -> None:
        self.spans: list[ReadableSpan] = []

    def export(self, spans) -> SpanExportResult:  # type: ignore[override]
        self.spans.extend(spans)
        return SpanExportResult.SUCCESS

    def shutdown(self) -> None:
        pass


class _Tee(io.TextIOBase):
    """Writes to both the capture buffer and the real stdout, so pytest's own output isn't silenced during this test."""

    def __init__(self, *targets: io.TextIOBase) -> None:
        self._targets = targets

    def write(self, s: str) -> int:
        for target in self._targets:
            target.write(s)
        return len(s)

    def flush(self) -> None:
        for target in self._targets:
            target.flush()


@pytest.mark.asyncio
async def test_no_secret_ever_appears_in_a_log_line_or_a_span_attribute() -> None:
    dsn = _dsn_or_skip()

    span_exporter = _CapturingSpanExporter()
    tracer_provider = trace.get_tracer_provider()
    if isinstance(tracer_provider, TracerProvider):
        tracer_provider.add_span_processor(SimpleSpanProcessor(span_exporter))

    stdout_buffer = io.StringIO()

    # Migrations off: other integration tests in this same session (see
    # tests/integration/conftest.py's `engine` fixture) create the schema
    # directly from `OrmBase.metadata` against the same shared database,
    # bypassing Alembic's own `alembic_version` bookkeeping entirely. Real
    # Alembic migrations (`create_app()`'s default) would then try to
    # `CREATE TABLE` from scratch and collide with tables that already
    # exist. `_ensure_schema` below creates the schema itself instead, the
    # same `checkfirst`-by-default, order-independent way the other
    # integration tests already do.
    settings = Settings(
        postgres_dsn=dsn,
        jwt_signing_key="MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=",
        jwt_issuer="stackbraid-tests",
        jwt_audience="stackbraid-tests",
        run_migrations_on_startup=False,
    )
    await _ensure_schema(dsn)

    password = "Sup3rSecretPassw0rd!!"
    email = f"redaction-{uuid.uuid4().hex}@example.com"

    root_logger = logging.getLogger()
    original_handlers = root_logger.handlers

    try:
        with contextlib.redirect_stdout(_Tee(stdout_buffer, sys.stdout)):
            test_app = create_app(settings=settings)

            async with test_app.router.lifespan_context(test_app):
                transport = httpx.ASGITransport(app=test_app)
                # https, not http: the refresh cookie is marked `Secure`
                # (matching the contract text exactly), and httpx's own
                # cookie jar — like a real browser — refuses to send a
                # `Secure` cookie back over a plain http request, even
                # in-process against an ASGI app that never actually
                # touches a socket.
                async with httpx.AsyncClient(transport=transport, base_url="https://testserver") as client:
                    register_response = await client.post(
                        "/v1/auth/register",
                        json={"email": email, "password": password, "displayName": "Redaction Test"},
                    )
                    assert register_response.status_code == 201

                    login_response = await client.post("/v1/auth/login", json={"email": email, "password": password})
                    assert login_response.status_code == 200
                    tokens = login_response.json()
                    cookie_value = client.cookies.get("refreshToken")
                    assert cookie_value

                    refresh_response = await client.post("/v1/auth/refresh")
                    assert refresh_response.status_code == 200
                    refreshed_tokens = refresh_response.json()

                    me_response = await client.get(
                        "/v1/auth/me",
                        headers={"Authorization": f"Bearer {refreshed_tokens['accessToken']}"},
                    )
                    assert me_response.status_code == 200

                    logout_response = await client.post(
                        "/v1/auth/logout",
                        headers={"Authorization": f"Bearer {refreshed_tokens['accessToken']}"},
                    )
                    assert logout_response.status_code == 204

                    correlation_id = refresh_response.headers.get("X-Correlation-Id")
    finally:
        # The OpenTelemetry SDK's periodic metric-export thread is a
        # process-wide singleton that outlives this test and keeps firing
        # on its own interval — restoring the root logger's handlers here
        # (rather than leaving them pointed at a buffer/stdout pairing
        # that is about to go out of scope) keeps that background thread's
        # own logging from raising once this test's capture is gone.
        root_logger.handlers = original_handlers

    captured_text = stdout_buffer.getvalue()

    secrets = [
        password,
        cookie_value,
        tokens["accessToken"],
        tokens["refreshToken"],
        refreshed_tokens["accessToken"],
        refreshed_tokens["refreshToken"],
        f"Bearer {refreshed_tokens['accessToken']}",
    ]

    for secret in secrets:
        assert secret not in captured_text, f"the process's own console output leaked a secret value: {secret!r}"

    for span in span_exporter.spans:
        for key, value in span.attributes.items():
            value_text = str(value)
            for secret in secrets:
                assert secret not in value_text, f"span '{span.name}' attribute '{key}' leaked a secret value: {value_text!r}"

    # The correlation ID itself is meant to travel everywhere — proving it
    # actually reached the captured output is what makes the absence of a
    # secret above meaningful, rather than the capture simply having caught
    # nothing at all.
    assert correlation_id
    assert correlation_id in captured_text, (
        "expected the request's own correlation ID to appear somewhere in the process's real console output "
        "(the per-request structured log line)"
    )
