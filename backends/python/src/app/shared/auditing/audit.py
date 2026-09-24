from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from typing import Protocol
from uuid import UUID


@dataclass(frozen=True, slots=True)
class AuditEntry:
    id: UUID
    entity_type: str
    entity_id: UUID
    action: str
    actor_id: UUID | None
    correlation_id: str
    occurred_at: datetime
    details: str | None


class AuditLog(Protocol):
    def add(self, entry: AuditEntry) -> None: ...

    async def list(self, entity_id: UUID) -> list[AuditEntry]: ...
