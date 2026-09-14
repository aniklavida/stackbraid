"""Integration tests run against a real, throwaway local Postgres cluster —
no Docker, no Testcontainers (see ``scripts/start-local-postgres.sh``, the
same no-Docker constraint the .NET backend's own integration tests follow).
Skipped automatically when no such database is configured, so the unit test
suite stays runnable with nothing but the Python interpreter.
"""

from __future__ import annotations

import os
from typing import AsyncIterator

import pytest
import pytest_asyncio
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from app.database.postgres.engine import create_engine
from app.shared.persistence.base import OrmBase
from app.features.identity.persistence import models  # noqa: F401 - registers ORM tables on OrmBase.metadata

DSN_ENV_VAR = "STACKBRAID_POSTGRES_DSN"


def _dsn_or_skip() -> str:
    dsn = os.environ.get(DSN_ENV_VAR)
    if not dsn:
        pytest.skip(f"{DSN_ENV_VAR} is not set — start a local test database with scripts/start-local-postgres.sh")
    return dsn


@pytest_asyncio.fixture(scope="session")
async def engine():
    dsn = _dsn_or_skip()
    eng = create_engine(dsn)

    async with eng.begin() as conn:
        await conn.run_sync(OrmBase.metadata.create_all)

    yield eng

    async with eng.begin() as conn:
        await conn.run_sync(OrmBase.metadata.drop_all)

    await eng.dispose()


@pytest_asyncio.fixture
async def session(engine) -> AsyncIterator[AsyncSession]:
    session_factory = async_sessionmaker(engine, expire_on_commit=False)
    async with session_factory() as s:
        yield s
        await s.rollback()
