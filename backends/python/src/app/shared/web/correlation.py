"""Every request gets one correlation ID — read from an inbound
``X-Correlation-Id`` header when a caller (or a gateway in front of this
service) already set one, otherwise minted here. It becomes the contract's
``Problem.traceId`` on any error response.
"""

from __future__ import annotations

import uuid

from starlette.middleware.base import BaseHTTPMiddleware, RequestResponseEndpoint
from starlette.requests import Request
from starlette.responses import Response

HEADER_NAME = "X-Correlation-Id"
_STATE_KEY = "correlation_id"


def get_correlation_id(request: Request) -> str:
    return getattr(request.state, _STATE_KEY, None) or request.headers.get(HEADER_NAME) or "unknown"


class CorrelationIdMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request: Request, call_next: RequestResponseEndpoint) -> Response:
        correlation_id = request.headers.get(HEADER_NAME) or uuid.uuid4().hex
        setattr(request.state, _STATE_KEY, correlation_id)

        response = await call_next(request)
        response.headers[HEADER_NAME] = correlation_id
        return response
