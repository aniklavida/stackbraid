"""The one piece of per-request state observability needs that isn't
already carried by an OpenTelemetry ``Span`` or a Starlette ``Request``:
the correlation ID, readable from a log call anywhere in the same request's
async task tree without threading a ``Request`` object through every
function signature.

A ``ContextVar`` propagates correctly across ``await`` within one task —
exactly what a log statement deep in a handler, running on the same task
the middleware started, needs.
"""

from __future__ import annotations

from contextvars import ContextVar

correlation_id_var: ContextVar[str | None] = ContextVar("correlation_id", default=None)
actor_id_var: ContextVar[object | None] = ContextVar("actor_id", default=None)
