"""Wires the Identity feature's ORM models to a real MySQL database. The only
module in the Python backend allowed to name the ``aiomysql``/``PyMySQL``
driver — see ``STRUCTURE.md``. A sibling of ``database/postgres`` and
``database/sqlserver`` with this same shape; nothing above this layer changes.
"""

from __future__ import annotations

from sqlalchemy.ext.asyncio import AsyncEngine, AsyncSession, async_sessionmaker, create_async_engine


def create_engine(dsn: str) -> AsyncEngine:
    return create_async_engine(dsn, pool_pre_ping=True)


def create_session_factory(engine: AsyncEngine) -> async_sessionmaker[AsyncSession]:
    return async_sessionmaker(engine, expire_on_commit=False)
