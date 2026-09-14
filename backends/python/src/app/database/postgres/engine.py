"""Wires the Identity feature's ORM models to a real Postgres database. The
only module in the Python backend allowed to say "postgres" as a driver
concern — see ``STRUCTURE.md``. Swapping providers means writing a sibling
``database/sqlserver`` or ``database/mysql`` with this same shape; nothing
above this layer changes.
"""

from __future__ import annotations

from sqlalchemy.ext.asyncio import AsyncEngine, AsyncSession, async_sessionmaker, create_async_engine


def create_engine(dsn: str) -> AsyncEngine:
    return create_async_engine(dsn, pool_pre_ping=True)


def create_session_factory(engine: AsyncEngine) -> async_sessionmaker[AsyncSession]:
    return async_sessionmaker(engine, expire_on_commit=False)
