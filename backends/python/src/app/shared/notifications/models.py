from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from typing import Protocol
from uuid import UUID


@dataclass(frozen=True, slots=True)
class NotificationMessage:
    user_id: UUID
    email: str
    title: str
    body: str
    data: dict[str, str] | None = None


class NotificationSender(Protocol):
    channel: str

    async def send(self, notification: NotificationMessage) -> None: ...


@dataclass(frozen=True, slots=True)
class StoredNotification:
    id: UUID
    user_id: UUID
    title: str
    body: str
    read: bool
    created_at: datetime


class NotificationStore(Protocol):
    async def add(self, notification: StoredNotification) -> None: ...

    async def list(self, user_id: UUID, page: int, page_size: int) -> list[StoredNotification]: ...

    async def count(self, user_id: UUID) -> int: ...


@dataclass(frozen=True, slots=True)
class DeviceTokenRegistration:
    id: UUID
    user_id: UUID
    token: str
    platform: str
    app_version: str
    registered_at: datetime
    updated_at: datetime


class DeviceTokenStore(Protocol):
    async def register(self, user_id: UUID, token: str, platform: str, app_version: str) -> DeviceTokenRegistration: ...

    async def refresh(self, user_id: UUID, device_id: UUID, token: str) -> DeviceTokenRegistration | None: ...

    async def revoke(self, user_id: UUID, device_id: UUID) -> bool: ...

    async def tokens_for(self, user_id: UUID) -> list[str]: ...
