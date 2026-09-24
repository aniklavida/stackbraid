from __future__ import annotations

from datetime import datetime
from typing import Protocol


class SoftDeleteService(Protocol):
    def delete(self, entity: object, now: datetime) -> None: ...

    def restore(self, entity: object, now: datetime) -> None: ...


class InMemorySoftDeleteService:
    def delete(self, entity: object, now: datetime) -> None:
        getattr(entity, "deactivate")(now)

    def restore(self, entity: object, now: datetime) -> None:
        getattr(entity, "restore")()
