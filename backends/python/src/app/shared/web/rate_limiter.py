from __future__ import annotations

from typing import Protocol


class RateLimiter(Protocol):
    def try_acquire(self, key: str) -> bool: ...


class RateLimitExceededError(Exception):
    pass
