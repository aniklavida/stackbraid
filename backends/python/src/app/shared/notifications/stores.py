from __future__ import annotations

import asyncio
from datetime import datetime, timezone
from uuid import UUID, uuid4

from app.shared.notifications.models import DeviceTokenRegistration, NotificationStore, StoredNotification


class InMemoryNotificationStore:
    def __init__(self) -> None:
        self._notifications: dict[UUID, StoredNotification] = {}
        self._lock = asyncio.Lock()

    async def add(self, notification: StoredNotification) -> None:
        async with self._lock:
            self._notifications[notification.id] = notification

    async def list(self, user_id: UUID, page: int, page_size: int) -> list[StoredNotification]:
        async with self._lock:
            items = [item for item in self._notifications.values() if item.user_id == user_id]
        items.sort(key=lambda item: item.created_at, reverse=True)
        return items[(page - 1) * page_size : page * page_size]

    async def count(self, user_id: UUID) -> int:
        async with self._lock:
            return sum(item.user_id == user_id for item in self._notifications.values())


class InMemoryDeviceTokenStore:
    def __init__(self) -> None:
        self._devices: dict[UUID, DeviceTokenRegistration] = {}
        self._lock = asyncio.Lock()

    async def register(self, user_id: UUID, token: str, platform: str, app_version: str) -> DeviceTokenRegistration:
        async with self._lock:
            if any(device.token == token for device in self._devices.values()):
                raise ValueError("The device token is already registered.")
            now = datetime.now(timezone.utc)
            device = DeviceTokenRegistration(uuid4(), user_id, token, platform, app_version, now, now)
            self._devices[device.id] = device
            return device

    async def refresh(self, user_id: UUID, device_id: UUID, token: str) -> DeviceTokenRegistration | None:
        async with self._lock:
            device = self._devices.get(device_id)
            if device is None or device.user_id != user_id:
                return None
            refreshed = DeviceTokenRegistration(
                device.id,
                device.user_id,
                token,
                device.platform,
                device.app_version,
                device.registered_at,
                datetime.now(timezone.utc),
            )
            self._devices[device_id] = refreshed
            return refreshed

    async def revoke(self, user_id: UUID, device_id: UUID) -> bool:
        async with self._lock:
            device = self._devices.get(device_id)
            if device is None or device.user_id != user_id:
                return False
            del self._devices[device_id]
            return True

    async def tokens_for(self, user_id: UUID) -> list[str]:
        async with self._lock:
            return [device.token for device in self._devices.values() if device.user_id == user_id]
