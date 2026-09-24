from __future__ import annotations

from datetime import datetime, timezone
from typing import Protocol


class DateTimeProvider(Protocol):
    def utc_now(self) -> datetime: ...


class SystemDateTimeProvider:
    def utc_now(self) -> datetime:
        return datetime.now(timezone.utc)
