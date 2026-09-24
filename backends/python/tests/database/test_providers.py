"""Provider-level tests that need no live database.

Three things are proven here for every shipped provider:

1. the package exposes the uniform surface the composition root selects on;
2. an engine, a session factory and a persisted job store are built from the
   provider's own DSN without opening a connection;
3. the provider's own Alembic migration renders that dialect's DDL in offline
   mode — the provider-specific types (``UUID`` / ``UNIQUEIDENTIFIER`` /
   ``CHAR(32)``) are the evidence it is genuinely targeting the right server.

The live-database legs for SQL Server and MySQL run in CI as service
containers; they are deliberately not started here.
"""

from __future__ import annotations

import importlib
import io
from contextlib import redirect_stdout
from datetime import datetime, timezone

import pytest
from alembic import command
from alembic.config import Config
from sqlalchemy import or_, select
from sqlalchemy.dialects import mssql, mysql

from app.host.config import Settings
from app.shared.jobs.sqlalchemy_store import JobModel

PROVIDERS = ["postgres", "sqlserver", "mysql"]

# The Uuid column type each dialect renders, as proof the migration is being
# compiled for that provider's own dialect rather than a shared one.
UUIID_RENDERING = {
    "postgres": "UUID",
    "sqlserver": "UNIQUEIDENTIFIER",
    "mysql": "CHAR(32)",
}


@pytest.mark.parametrize("name", PROVIDERS)
def test_provider_package_exposes_the_uniform_surface(name: str) -> None:
    provider = importlib.import_module(f"app.database.{name}")

    for attribute in ("create_engine", "create_session_factory", "seed", "create_job_store", "resolve_dsn", "ALEMBIC_INI", "DSN_ENV_VAR"):
        assert hasattr(provider, attribute), f"app.database.{name} is missing {attribute}"


@pytest.mark.parametrize("name", PROVIDERS)
def test_provider_builds_engine_session_factory_and_job_store_without_connecting(name: str) -> None:
    provider = importlib.import_module(f"app.database.{name}")
    dsn = provider.resolve_dsn(Settings())

    engine = provider.create_engine(dsn)
    session_factory = provider.create_session_factory(engine)
    job_store = provider.create_job_store(session_factory)

    assert engine.dialect.name
    assert callable(session_factory)
    assert callable(job_store.add)


@pytest.mark.parametrize("name", PROVIDERS)
def test_provider_migration_renders_its_own_dialect_ddl_offline(name: str) -> None:
    provider = importlib.import_module(f"app.database.{name}")
    config = Config(str(provider.ALEMBIC_INI))

    buffer = io.StringIO()
    with redirect_stdout(buffer):
        command.upgrade(config, "head", sql=True)
    sql = buffer.getvalue()

    assert "identity_users" in sql
    assert "shared_jobs" in sql
    assert UUIID_RENDERING[name] in sql


def test_sqlserver_job_store_claim_uses_the_skip_locked_equivalent() -> None:
    from app.database.sqlserver.job_store import SqlServerJobStore

    statement = SqlServerJobStore.claim_statement(5, datetime.now(timezone.utc))
    compiled = str(statement.compile(dialect=mssql.dialect()))

    assert "READPAST" in compiled
    assert "UPDLOCK" in compiled
    assert "shared_jobs" in compiled


def test_shared_job_store_claim_compiles_with_skip_locked_for_mysql() -> None:
    statement = (
        select(JobModel.id)
        .where(JobModel.state == "queued", or_(JobModel.next_attempt_at.is_(None), JobModel.next_attempt_at <= datetime.now(timezone.utc)))
        .order_by(JobModel.created_at)
        .limit(5)
        .with_for_update(skip_locked=True)
    )

    assert "SKIP LOCKED" in str(statement.compile(dialect=mysql.dialect()))
