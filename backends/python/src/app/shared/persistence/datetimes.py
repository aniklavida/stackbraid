"""Normalizes timestamps read back from a database to timezone-aware UTC.

Postgres and SQL Server return timezone-aware values for the
``DateTime(timezone=True)`` columns this backend uses, but MySQL's DATETIME
type stores no timezone and its driver hands back naive values. The domain
compares timestamps — a refresh token's expiry against "now", for instance —
and comparing a naive value with an aware one raises ``TypeError``. The
repository is the mapping boundary between a row and a domain entity, so it
is the right place to normalize; nothing above it has to know which provider
is underneath.
"""

from __future__ import annotations

from datetime import datetime, timezone


def ensure_utc(value: datetime) -> datetime:
    if value.tzinfo is None:
        return value.replace(tzinfo=timezone.utc)
    return value.astimezone(timezone.utc)


def ensure_utc_optional(value: datetime | None) -> datetime | None:
    return None if value is None else ensure_utc(value)
