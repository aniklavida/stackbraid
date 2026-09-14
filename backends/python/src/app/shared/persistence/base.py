"""The provider-agnostic base every feature's ORM mapping starts from —
nothing about which database is underneath, and nothing about any feature's
entity shapes. A feature's own audit-timestamp and domain-event handling
lives in that feature's own persistence module, not here — ``Domain``
depends on nothing, so those concerns can never leak inward.
"""

from __future__ import annotations

from sqlalchemy.orm import DeclarativeBase


class OrmBase(DeclarativeBase):
    """The single declarative base every provider-agnostic ORM model in this
    codebase inherits from, so Alembic's autogenerate sees one shared
    metadata object regardless of which feature defined the table.
    """
