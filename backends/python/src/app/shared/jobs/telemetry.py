"""The one place background jobs emit traces and metrics — the Python twin
of the .NET backend's ``Shared/Jobs/JobTelemetry.cs``. The worker opens a
span per attempt whose remote parent is the trace that enqueued the job, so
the job's span and its originating request share one trace, and it restores
the request's correlation ID into the logging context so the worker's own
log lines are findable from the request. Instrument names here are the ones
the checked-in Grafana dashboard queries — see
``infra/grafana/dashboards/background-jobs.json``.
"""

from __future__ import annotations

import contextlib
from collections.abc import Iterator
from typing import Any

from opentelemetry import metrics, trace
from opentelemetry.trace import SpanKind
from opentelemetry.trace.propagation.tracecontext import TraceContextTextMapPropagator

from app.shared.observability.context import correlation_id_var

METER_NAME = "StackBraid.Jobs"
TRACER_NAME = "StackBraid.Jobs"

CORRELATION_ID_ATTRIBUTE = "app.correlation_id"
JOB_TYPE_ATTRIBUTE = "job.type"
JOB_OUTCOME_ATTRIBUTE = "job.outcome"
FAILURE_REASON_ATTRIBUTE = "job.reason"

SUCCEEDED_OUTCOME = "succeeded"
RETRYING_OUTCOME = "retrying"
DEAD_LETTERED_OUTCOME = "dead-lettered"

HANDLER_ERROR_REASON = "handler-error"
NO_HANDLER_REASON = "no-handler"

_tracer = trace.get_tracer(TRACER_NAME)
_meter = metrics.get_meter(METER_NAME)

job_duration = _meter.create_histogram(
    "stackbraid.jobs.duration",
    unit="s",
    description="Time a background job attempt took to run.",
)
job_failures = _meter.create_counter(
    "stackbraid.jobs.failures",
    description="Background job attempts that failed.",
)

_propagator = TraceContextTextMapPropagator()


def current_context() -> tuple[str | None, str | None, str | None]:
    """The ambient ``(correlation_id, traceparent, tracestate)`` to store with a job.

    Both are read from the enqueuing request: the correlation ID from the
    context variable the middleware sets, the trace context from the current
    span through the same W3C propagator every OpenTelemetry SDK uses.
    """
    carrier: dict[str, str] = {}
    _propagator.inject(carrier)
    return correlation_id_var.get(), carrier.get("traceparent"), carrier.get("tracestate")


def _extract_parent(trace_parent: str | None, trace_state: str | None) -> Any:
    if not trace_parent:
        return None
    return _propagator.extract({"traceparent": trace_parent, "tracestate": trace_state or ""})


@contextlib.contextmanager
def job_span(job: Any) -> Iterator[Any]:
    """Run a job attempt inside a span joined to its enqueuing request's trace.

    The correlation ID is set for the duration so every log line the worker
    or handler emits inside this block carries it too, exactly as a request's
    own log lines do.
    """
    parent = _extract_parent(job.trace_parent, job.trace_state)
    token = correlation_id_var.set(job.correlation_id) if job.correlation_id else None
    try:
        with _tracer.start_as_current_span("job.execute", context=parent, kind=SpanKind.CONSUMER) as span:
            span.set_attribute(JOB_TYPE_ATTRIBUTE, job.type)
            if job.correlation_id:
                span.set_attribute(CORRELATION_ID_ATTRIBUTE, job.correlation_id)
            yield span
    finally:
        if token is not None:
            correlation_id_var.reset(token)


def record_duration(job_type: str, outcome: str, seconds: float) -> None:
    job_duration.record(seconds, attributes={JOB_TYPE_ATTRIBUTE: job_type, JOB_OUTCOME_ATTRIBUTE: outcome})


def record_failure(job_type: str, reason: str) -> None:
    job_failures.add(1, attributes={JOB_TYPE_ATTRIBUTE: job_type, FAILURE_REASON_ATTRIBUTE: reason})
