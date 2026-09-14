"""The one rate-limit policy every backend needs on day one: authentication
endpoints, keyed by client IP, so credential stuffing against
``/v1/auth/login`` gets a 429 (see ``contract/openapi.yaml``'s
``TooManyRequests`` response) instead of unlimited attempts.

A fixed-window counter, held in process memory — correct for a
single-instance deployment and an honest placeholder rather than a Redis-
backed limiter this skeleton doesn't yet run, exactly like .NET's built-in
fixed-window limiter it mirrors.
"""

from __future__ import annotations

import time
from collections import defaultdict
from dataclasses import dataclass

from fastapi import Depends, Request
from starlette.responses import JSONResponse

from app.shared.localization.localizer import AppLocalizer
from app.shared.web.errors import AppError, AppErrorType
from app.shared.web.problem import problem_response


class RateLimitExceededError(Exception):
    """Raised by ``enforce_auth_rate_limit`` and translated into a 429
    Problem response by the handler registered in ``host/main.py`` — never a
    bare status code with no body."""


@dataclass
class _Window:
    started_at: float
    count: int


class FixedWindowRateLimiter:
    """High enough that a legitimate burst — a test suite, or one user's
    browser retrying login/refresh/register in quick succession — never trips
    it, while still bounding genuine credential-stuffing volume.
    """

    def __init__(self, permit_limit: int = 100, window_seconds: float = 60.0) -> None:
        self._permit_limit = permit_limit
        self._window_seconds = window_seconds
        self._windows: dict[str, _Window] = defaultdict(lambda: _Window(time.monotonic(), 0))

    def try_acquire(self, key: str) -> bool:
        now = time.monotonic()
        window = self._windows[key]
        if now - window.started_at >= self._window_seconds:
            window.started_at = now
            window.count = 0

        window.count += 1
        return window.count <= self._permit_limit


def rate_limited_response(request: Request, localizer: AppLocalizer) -> JSONResponse:
    error = AppError.validation("IDENTITY.RATE_LIMITED", "identity.rate_limited", {})
    error = AppError(error.code, AppErrorType.FAILURE, error.message_key)
    return problem_response(error, localizer, request, status_override=429)


def client_key(request: Request) -> str:
    return request.client.host if request.client else "unknown"


def get_auth_rate_limiter(request: Request) -> FixedWindowRateLimiter:
    return request.app.state.auth_rate_limiter  # type: ignore[no-any-return]


def enforce_auth_rate_limit(request: Request, limiter: FixedWindowRateLimiter = Depends(get_auth_rate_limiter)) -> None:
    if not limiter.try_acquire(client_key(request)):
        raise RateLimitExceededError()
