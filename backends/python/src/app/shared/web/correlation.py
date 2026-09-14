"""Every request gets one correlation ID — read from an inbound
``X-Correlation-Id`` header when a caller (or a gateway in front of this
service) already set one, otherwise minted here. It becomes the contract's
``Problem.traceId`` on any error response.
"""

from __future__ import annotations

import logging
import time
import uuid

from opentelemetry import trace
from starlette.middleware.base import BaseHTTPMiddleware, RequestResponseEndpoint
from starlette.requests import Request
from starlette.responses import Response

from app.shared.observability.context import correlation_id_var

HEADER_NAME = "X-Correlation-Id"
_STATE_KEY = "correlation_id"

_logger = logging.getLogger("stackbraid.request")


def get_correlation_id(request: Request) -> str:
    return getattr(request.state, _STATE_KEY, None) or request.headers.get(HEADER_NAME) or "unknown"


class CorrelationIdMiddleware(BaseHTTPMiddleware):
    """Every request gets one correlation ID — read from an inbound
    ``X-Correlation-Id`` header when a caller (or a gateway in front of
    this service) already set one, otherwise minted here. It becomes the
    contract's ``Problem.traceId`` on any error response.

    It is also stamped onto the current OpenTelemetry span as
    ``app.correlation_id``, published through a ``ContextVar`` so every log
    line for the rest of this request carries it (see
    ``shared/observability/logging_setup.py``), and closes with one
    structured "request handled" log line — the same rule the .NET
    backend's own ``CorrelationIdMiddleware`` follows, so a trace exported
    to a console/file/OTLP backend and a log line on disk can each be found
    from the other.
    """

    async def dispatch(self, request: Request, call_next: RequestResponseEndpoint) -> Response:
        correlation_id = request.headers.get(HEADER_NAME) or uuid.uuid4().hex
        setattr(request.state, _STATE_KEY, correlation_id)
        token = correlation_id_var.set(correlation_id)

        span = trace.get_current_span()
        span.set_attribute("app.correlation_id", correlation_id)

        started_at = time.perf_counter()
        try:
            response = await call_next(request)
            elapsed_ms = (time.perf_counter() - started_at) * 1000
            response.headers[HEADER_NAME] = correlation_id

            _logger.info(
                "Handled %s %s -> %s in %.2fms",
                request.method,
                request.url.path,
                response.status_code,
                elapsed_ms,
                extra={
                    "requestMethod": request.method,
                    "requestPath": request.url.path,
                    "statusCode": response.status_code,
                    "elapsedMilliseconds": elapsed_ms,
                },
            )
            return response
        finally:
            correlation_id_var.reset(token)
