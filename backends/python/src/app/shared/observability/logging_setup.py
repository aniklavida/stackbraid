"""Structured (JSON, one object per line) logging, matching the .NET
backend's Serilog console output in shape rather than byte-for-byte format:
every line carries the correlation ID plus the current span's trace/span
id, so a log line and an exported trace can each be found from the other —
search logs for the correlation ID to get the trace ID, or open the trace
to read the correlation ID back off it.

Hand-rolled rather than a third-party JSON-logging package: the exact same
"one flat key-value catalogue, stdlib only" choice already made for
``shared/localization/localizer.py``, for the same reason — this backend's
own needs are simple enough that a dependency (with its own licence to
audit) buys nothing a few dozen lines of stdlib ``logging`` doesn't already
provide.
"""

from __future__ import annotations

import json
import logging
import sys
from datetime import datetime, timezone

from opentelemetry import trace

from app.shared.observability.context import correlation_id_var

_RESERVED_RECORD_ATTRS = frozenset(logging.LogRecord("", 0, "", 0, "", (), None).__dict__.keys())


class JsonFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        payload: dict[str, object] = {
            "timestamp": datetime.fromtimestamp(record.created, tz=timezone.utc).isoformat(),
            "level": record.levelname,
            "logger": record.name,
            "message": record.getMessage(),
        }

        correlation_id = correlation_id_var.get()
        if correlation_id is not None:
            payload["correlationId"] = correlation_id

        span = trace.get_current_span()
        span_context = span.get_span_context()
        if span_context.is_valid:
            payload["traceId"] = format(span_context.trace_id, "032x")
            payload["spanId"] = format(span_context.span_id, "016x")

        # Any extra field passed via `logger.info(..., extra={...})` rides
        # along too, the same way Serilog's structured properties do —
        # `_RESERVED_RECORD_ATTRS` is what a bare `LogRecord` already
        # defines, so only genuinely extra fields get added here.
        for key, value in record.__dict__.items():
            if key not in _RESERVED_RECORD_ATTRS:
                payload[key] = value

        if record.exc_info:
            payload["exception"] = self.formatException(record.exc_info)

        return json.dumps(payload, default=str)


def configure_logging(level: int = logging.INFO) -> None:
    root = logging.getLogger()
    root.setLevel(level)

    # Idempotent: a second call (a test creating a second app instance,
    # for example) replaces the handler instead of stacking a duplicate
    # one that would double every line.
    root.handlers = [_console_handler()]

    # Uvicorn installs its own access-log handler with a different
    # formatter by default; routing it through the same JSON formatter
    # keeps every line — framework or application — in one consistent
    # shape.
    for name in ("uvicorn", "uvicorn.error", "uvicorn.access"):
        logger = logging.getLogger(name)
        logger.handlers = []
        logger.propagate = True


def _console_handler() -> logging.Handler:
    handler = logging.StreamHandler(stream=sys.stdout)
    handler.setFormatter(JsonFormatter())
    return handler
