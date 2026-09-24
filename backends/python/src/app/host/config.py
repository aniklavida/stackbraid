"""Configuration, read from the environment — the composition root's own
concern, never imported by a feature.
"""

from __future__ import annotations

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="STACKBRAID_", extra="ignore")

    postgres_dsn: str = "postgresql+asyncpg://postgres:postgres@127.0.0.1:5432/stackbraid"

    # A fixed, publicly-known development default — never used past local
    # development. Base64-encoded HMAC-SHA256 key, same shape .NET's own
    # development default takes.
    jwt_signing_key: str = "ZGV2ZWxvcG1lbnQtb25seS1zaWduaW5nLWtleS1kby1ub3QtdXNlLWluLXByb2Q="
    jwt_issuer: str = "stackbraid"
    jwt_audience: str = "stackbraid-clients"
    jwt_access_token_lifetime_seconds: int = 900

    auth_rate_limit_permits_per_minute: int = 100

    # Left empty by default — the realtime layer delivers to clients
    # connected to *this* process with no backplane at all, correct for
    # one instance. Set to a Redis (or Valkey — both speak the same wire
    # protocol) connection URL, e.g. "redis://127.0.0.1:6379", to fan a
    # message out to every instance behind the same one.
    realtime_redis_url: str = ""

    run_migrations_on_startup: bool = True
    seed_on_startup: bool = True

    # Path to the authoritative OpenAPI contract file. Defaults to empty, which
    # resolves contract/openapi.yaml relative to the repository tree or working directory.
    contract_path: str = ""

    # Comma-separated origins allowed to make cross-origin, credentialed
    # requests (e.g. a frontend's dev server calling this API from
    # a different port). Empty by default — no frontend origin is trusted
    # until it is listed explicitly.
    cors_allowed_origins_raw: str = ""

    @property
    def cors_allowed_origins(self) -> list[str]:
        return [origin.strip() for origin in self.cors_allowed_origins_raw.split(",") if origin.strip()]

    # One instrumentation layer this process can point anywhere, rather
    # than a hard wiring to one vendor: traces and metrics always go to
    # the console (verifiable locally with no collector), and are
    # *additionally* exported over OTLP only when this is actually set —
    # this process never guesses at or dials a collector nobody asked it
    # to. Read directly from the standard `OTEL_EXPORTER_OTLP_ENDPOINT`
    # env var (no `STACKBRAID_` prefix — `validation_alias` reads this
    # exact name, bypassing the class-wide prefix below) since that is the
    # name every OpenTelemetry SDK, in any language, already agrees on.
    smtp_host: str = ""
    smtp_port: int = 587
    smtp_username: str = ""
    smtp_password: str = ""
    smtp_from_address: str = "no-reply@example.com"

    firebase_project_id: str = ""
    firebase_access_token: str = ""

    otel_otlp_endpoint: str = Field(default="", validation_alias="OTEL_EXPORTER_OTLP_ENDPOINT")
