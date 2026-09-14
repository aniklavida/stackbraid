"""An in-process cache with per-key TTL. Swap this registration for a
Redis-backed implementation without touching a single caller — nothing here
assumes a single-instance deployment except the fact that it is one.
"""

from __future__ import annotations

import time
from typing import Any, Awaitable, Callable, Protocol, TypeVar

T = TypeVar("T")


class Cache(Protocol):
    async def get_or_create(self, key: str, factory: Callable[[], Awaitable[T]], ttl_seconds: float) -> T: ...

    def remove(self, key: str) -> None: ...


class InMemoryCache:
    def __init__(self) -> None:
        self._entries: dict[str, tuple[float, Any]] = {}

    async def get_or_create(self, key: str, factory: Callable[[], Awaitable[T]], ttl_seconds: float) -> T:
        now = time.monotonic()
        cached = self._entries.get(key)
        if cached is not None and cached[0] > now:
            return cached[1]

        value = await factory()
        self._entries[key] = (now + ttl_seconds, value)
        return value

    def remove(self, key: str) -> None:
        self._entries.pop(key, None)
