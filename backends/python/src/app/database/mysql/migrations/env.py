"""Alembic environment for MySQL — the only place this provider's migration
wiring lives. Runs asynchronously against the same ``aiomysql`` driver the
application uses (backed by pure-Python ``PyMySQL``), via
``AsyncEngine.run_sync`` (Alembic's own documented recipe for an async
dialect), so no second, sync-only driver needs to be installed.
"""

from __future__ import annotations

import asyncio
import os
from logging.config import fileConfig

from alembic import context
from sqlalchemy import Connection, pool
from sqlalchemy.ext.asyncio import async_engine_from_config

# Import every feature's ORM models here so Alembic's autogenerate sees the
# full metadata — this is the one file allowed to know about every feature's
# persistence models, since it lives in database/mysql/, not in a feature.
from app.features.identity.persistence import models  # noqa: F401
from app.shared.jobs import sqlalchemy_store  # noqa: F401  (registers shared_jobs)
from app.shared.persistence.base import OrmBase

config = context.config

if config.config_file_name is not None:
    fileConfig(config.config_file_name)

target_metadata = OrmBase.metadata


def _dsn() -> str:
    return os.environ.get(
        "STACKBRAID_MYSQL_DSN",
        "mysql+aiomysql://root:ChangeMe%21123@127.0.0.1:3306/stackbraid?charset=utf8mb4",
    )


def run_migrations_offline() -> None:
    context.configure(
        url=_dsn(),
        target_metadata=target_metadata,
        literal_binds=True,
        dialect_opts={"paramstyle": "named"},
    )
    with context.begin_transaction():
        context.run_migrations()


def _do_run_migrations(connection: Connection) -> None:
    context.configure(connection=connection, target_metadata=target_metadata)
    with context.begin_transaction():
        context.run_migrations()


async def run_migrations_online() -> None:
    configuration = config.get_section(config.config_ini_section) or {}
    configuration["sqlalchemy.url"] = _dsn()

    connectable = async_engine_from_config(configuration, prefix="sqlalchemy.", poolclass=pool.NullPool)

    async with connectable.connect() as connection:
        await connection.run_sync(_do_run_migrations)

    await connectable.dispose()


if context.is_offline_mode():
    run_migrations_offline()
else:
    asyncio.run(run_migrations_online())
