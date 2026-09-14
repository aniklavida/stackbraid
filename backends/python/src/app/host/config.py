"""Configuration, read from the environment — the composition root's own
concern, never imported by a feature.
"""

from __future__ import annotations

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

    run_migrations_on_startup: bool = True
    seed_on_startup: bool = True
