"""The correlation ID and trace context a request already carries must survive
the jump into background work: a job enqueued over real HTTP is stored with
that request's correlation ID and W3C trace context, and the worker's own span
and log line carry both — the worker's span joins the originating request's
trace, and its log line names the request's correlation ID.

The .NET backend's own
``Features.Identity.IntegrationTests.JobObservabilityIntegrationTests`` proves
the same thing there; this is the Python-side counterpart. Driven against the
real running app (real Postgres, real JWT issuance, real OpenTelemetry
instrumentation, real structured logging) with additive capture points:
everything written to ``sys.stdout``, a span processor added onto the existing
``TracerProvider``, and an in-memory metric reader added onto the existing
``MeterProvider``.
"""

from __future__ import annotations

import contextlib
import io
import json
import logging
import sys
import uuid

import httpx
import pytest
from opentelemetry import metrics, trace
from opentelemetry.sdk.metrics import MeterProvider
from opentelemetry.sdk.metrics.export import InMemoryMetricReader
from opentelemetry.sdk.trace import ReadableSpan, TracerProvider
from opentelemetry.sdk.trace.export import SimpleSpanProcessor, SpanExporter, SpanExportResult

from app.database.postgres.engine import create_engine
from app.features.identity.persistence import models  # noqa: F401 - registers ORM tables on OrmBase.metadata
from app.host.config import Settings
from app.host.main import create_app
from app.shared.jobs.conformance import ConformanceDeadLetterJobHandler, ConformanceSucceedsJobHandler
from app.shared.jobs import telemetry
from app.shared.persistence.base import OrmBase
from tests.integration.conftest import _dsn_or_skip


async def _ensure_schema(dsn: str) -> None:
    engine = create_engine(dsn)
    async with engine.begin() as conn:
        await conn.run_sync(OrmBase.metadata.create_all)
    await engine.dispose()


class _CapturingSpanExporter(SpanExporter):
    def __init__(self) -> None:
        self.spans: list[ReadableSpan] = []

    def export(self, spans) -> SpanExportResult:  # type: ignore[override]
        self.spans.extend(spans)
        return SpanExportResult.SUCCESS

    def shutdown(self) -> None:
        pass


class _Tee(io.TextIOBase):
    """Writes to both the capture buffer and the real stdout, so pytest's own output isn't silenced."""

    def __init__(self, *targets: io.TextIOBase) -> None:
        self._targets = targets

    def write(self, s: str) -> int:
        for target in self._targets:
            target.write(s)
        return len(s)

    def flush(self) -> None:
        for target in self._targets:
            target.flush()


def _log_lines(buffer: io.StringIO) -> list[dict]:
    records = []
    for line in buffer.getvalue().splitlines():
        line = line.strip()
        if not line.startswith("{"):
            continue
        try:
            records.append(json.loads(line))
        except json.JSONDecodeError:
            continue
    return records


def _has_metric_point(reader: InMemoryMetricReader, name: str, expected: dict[str, str]) -> bool:
    data = reader.get_metrics_data()
    if data is None:
        return False
    for resource_metrics in data.resource_metrics:
        for scope_metrics in resource_metrics.scope_metrics:
            for metric in scope_metrics.metrics:
                if metric.name != name:
                    continue
                for point in metric.data.data_points:
                    attributes = dict(point.attributes)
                    if all(attributes.get(key) == value for key, value in expected.items()):
                        return True
    return False


@pytest.mark.asyncio
async def test_a_jobs_worker_span_and_log_line_carry_the_enqueuing_requests_correlation_id_and_trace_id() -> None:
    dsn = _dsn_or_skip()

    stdout_buffer = io.StringIO()
    settings = Settings(
        postgres_dsn=dsn,
        jwt_signing_key="MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=",
        jwt_issuer="stackbraid-tests",
        jwt_audience="stackbraid-tests",
        run_migrations_on_startup=False,
        jobs_conformance_enabled=True,
        jobs_poll_interval_seconds=0.05,
        jobs_base_retry_delay_seconds=0.05,
        jobs_max_retry_delay_seconds=1.0,
    )
    await _ensure_schema(dsn)

    password = "Sup3rSecretPassw0rd!!"
    email = f"job-observability-{uuid.uuid4().hex}@example.com"
    correlation_id = uuid.uuid4().hex

    root_logger = logging.getLogger()
    original_handlers = root_logger.handlers
    span_exporter = _CapturingSpanExporter()
    metric_reader = InMemoryMetricReader()

    try:
        with contextlib.redirect_stdout(_Tee(stdout_buffer, sys.stdout)):
            test_app = create_app(settings=settings)

            tracer_provider = trace.get_tracer_provider()
            if isinstance(tracer_provider, TracerProvider):
                tracer_provider.add_span_processor(SimpleSpanProcessor(span_exporter))
            meter_provider = metrics.get_meter_provider()
            if isinstance(meter_provider, MeterProvider):
                meter_provider.add_metric_reader(metric_reader)

            async with test_app.router.lifespan_context(test_app):
                transport = httpx.ASGITransport(app=test_app)
                async with httpx.AsyncClient(transport=transport, base_url="https://testserver") as client:
                    register = await client.post(
                        "/v1/auth/register",
                        json={"email": email, "password": password, "displayName": "Job Observability"},
                    )
                    assert register.status_code == 201

                    login = await client.post("/v1/auth/login", json={"email": email, "password": password})
                    assert login.status_code == 200
                    access_token = login.json()["accessToken"]
                    headers = {"Authorization": f"Bearer {access_token}", "X-Correlation-Id": correlation_id}

                    enqueue = await client.post("/v1/jobs/scenarios", json={"scenario": "succeeds"}, headers=headers)
                    assert enqueue.status_code == 202
                    job_id = enqueue.json()["jobId"]

                    failing = await client.post("/v1/jobs/scenarios", json={"scenario": "dead-letter"}, headers=headers)
                    assert failing.status_code == 202

                    await _wait_for_terminal(client, job_id, headers)
    finally:
        root_logger.handlers = original_handlers

    # The worker's span joins the request's trace and carries the correlation ID.
    request_spans = [
        span
        for span in span_exporter.spans
        if span.name != "job.execute" and span.attributes.get(telemetry.CORRELATION_ID_ATTRIBUTE) == correlation_id
    ]
    assert request_spans, "the enqueuing request's own span should carry the correlation ID"
    request_trace_id = format(request_spans[0].context.trace_id, "032x")

    job_spans = [
        span
        for span in span_exporter.spans
        if span.name == "job.execute" and span.attributes.get(telemetry.CORRELATION_ID_ATTRIBUTE) == correlation_id
    ]
    assert job_spans, "the worker's span should carry the request's correlation ID"
    assert format(job_spans[0].context.trace_id, "032x") == request_trace_id

    # The worker's own log line for this job names the request's correlation ID
    # and the trace it belongs to.
    worker_lines = [
        record
        for record in _log_lines(stdout_buffer)
        if job_id in str(record.get("message", "")) and "succeeded" in str(record.get("message", ""))
    ]
    assert worker_lines, "the worker should have logged the completed job"
    worker_line = worker_lines[0]
    assert worker_line.get("correlationId") == correlation_id
    assert worker_line.get("traceId") == request_trace_id

    # The duration histogram and the failure counter are emitted by the worker.
    assert _has_metric_point(
        metric_reader,
        "stackbraid.jobs.duration",
        {telemetry.JOB_TYPE_ATTRIBUTE: ConformanceSucceedsJobHandler.job_type, telemetry.JOB_OUTCOME_ATTRIBUTE: telemetry.SUCCEEDED_OUTCOME},
    )
    assert _has_metric_point(
        metric_reader,
        "stackbraid.jobs.failures",
        {telemetry.JOB_TYPE_ATTRIBUTE: ConformanceDeadLetterJobHandler.job_type, telemetry.FAILURE_REASON_ATTRIBUTE: telemetry.HANDLER_ERROR_REASON},
    )


async def _wait_for_terminal(client: httpx.AsyncClient, job_id: str, headers: dict[str, str]) -> None:
    import asyncio

    for _ in range(200):
        status = await client.get(f"/v1/jobs/{job_id}", headers=headers)
        assert status.status_code == 200
        if status.json()["status"] in {"succeeded", "dead-lettered"}:
            return
        await asyncio.sleep(0.1)
    raise TimeoutError(f"Job {job_id} did not reach a terminal state in time.")
