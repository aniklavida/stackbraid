"""The persisted scheduler and its worker — the Python twin of the .NET
backend's ``PersistentJobScheduler``/``JobWorker``. ``enqueue_request``
writes through the store before returning, so the queued work is still there
after the process restarts; the worker retries a failure with exponential
backoff until the attempt budget is spent, then dead-letters it.
"""

from __future__ import annotations

import asyncio
import logging
import time
from datetime import datetime, timedelta, timezone
from typing import Callable
from uuid import UUID, uuid4

from app.shared.jobs import telemetry
from app.shared.jobs.handlers import JobContext, JobHandlerRegistry, JobPayload
from app.shared.jobs.models import BackoffPolicy, JobRecord, JobRequest, JobStates, JobStatus, JobWorkerOptions
from app.shared.jobs.scheduler import Job, InProcessJobScheduler
from app.shared.jobs.store import JobStore

logger = logging.getLogger("stackbraid.jobs")


class PersistentJobScheduler:
    def __init__(
        self,
        store: JobStore,
        ephemeral: InProcessJobScheduler | None = None,
        now: Callable[[], datetime] | None = None,
    ) -> None:
        self._store = store
        self._ephemeral = ephemeral or InProcessJobScheduler()
        self._now = now or (lambda: datetime.now(timezone.utc))

    def enqueue(self, job: Job) -> None:
        """The fire-and-forget delegate path — kept ephemeral and in-process."""
        self._ephemeral.enqueue(job)

    async def enqueue_request(self, request: JobRequest) -> UUID:
        now = self._now()
        # Captured from the enqueuing request's ambient context — its span
        # already carries the correlation ID and the W3C trace context, so the
        # worker can restore both and join the same trace.
        correlation_id, trace_parent, trace_state = telemetry.current_context()
        record = JobRecord(
            id=uuid4(),
            type=request.type,
            payload_json=request.payload_json,
            owner_id=request.owner_id,
            state=JobStates.QUEUED,
            attempts=0,
            max_attempts=request.max_attempts,
            last_error=None,
            created_at=now,
            updated_at=now,
            correlation_id=correlation_id,
            trace_parent=trace_parent,
            trace_state=trace_state,
        )
        await self._store.add(record)
        return record.id

    async def status(self, job_id: UUID) -> JobStatus | None:
        record = await self._store.get(job_id)
        return record.to_status() if record is not None else None

    async def run_ephemeral_forever(self) -> None:
        await self._ephemeral.run_forever()


class JobWorker:
    def __init__(
        self,
        store: JobStore,
        registry: JobHandlerRegistry,
        options: JobWorkerOptions | None = None,
        now: Callable[[], datetime] | None = None,
    ) -> None:
        self._store = store
        self._registry = registry
        self.options = options or JobWorkerOptions()
        self._now = now or (lambda: datetime.now(timezone.utc))

    async def run_due(self) -> int:
        now = self._now()
        due = await self._store.claim_due(self.options.batch_size, now)
        for job in due:
            await self._execute(job)
        return len(due)

    async def run_forever(self) -> None:
        if not self.options.worker_enabled:
            logger.info("Job worker is disabled in this process; a dedicated worker is expected to drain the queue.")
            return
        while True:
            try:
                await self.run_due()
            except Exception:  # noqa: BLE001 - one bad pass must not kill the loop
                logger.exception("The job worker pass failed; it will retry on the next poll.")
            await asyncio.sleep(self.options.poll_interval_seconds)

    async def _execute(self, job: JobRecord) -> None:
        # The job's own span joins the trace that enqueued it (the persisted
        # traceparent is its remote parent) and both it and every log line
        # emitted below carry the correlation ID captured at enqueue time.
        started = time.perf_counter()
        with telemetry.job_span(job):
            handler = self._registry.resolve(job.type)
            if handler is None:
                outcome = await self._fail(job, f"No handler is registered for job type '{job.type}'.", telemetry.NO_HANDLER_REASON)
                telemetry.record_duration(job.type, outcome, time.perf_counter() - started)
                return

            try:
                await handler.handle(JobContext(job.id, job.type, job.owner_id, job.attempts), JobPayload(job.payload_json))
                job.state = JobStates.SUCCEEDED
                job.last_error = None
                job.next_attempt_at = None
                job.updated_at = self._now()
                await self._store.update(job)
                telemetry.record_duration(job.type, telemetry.SUCCEEDED_OUTCOME, time.perf_counter() - started)
                logger.info("Job %s (%s) succeeded on attempt %s.", job.id, job.type, job.attempts)
            except Exception as exc:  # noqa: BLE001 - any handler failure is a retryable attempt
                outcome = await self._fail(job, str(exc), telemetry.HANDLER_ERROR_REASON)
                telemetry.record_duration(job.type, outcome, time.perf_counter() - started)

    async def _fail(self, job: JobRecord, error: str, reason: str) -> str:
        now = self._now()
        job.last_error = error
        job.updated_at = now
        telemetry.record_failure(job.type, reason)

        if job.attempts >= job.max_attempts:
            job.state = JobStates.DEAD_LETTERED
            job.next_attempt_at = None
            await self._store.update(job)
            logger.error("Job %s (%s) exhausted its %s attempt(s) and was dead-lettered: %s", job.id, job.type, job.attempts, error)
            return telemetry.DEAD_LETTERED_OUTCOME

        delay = BackoffPolicy.compute(job.attempts, self.options.base_retry_delay_seconds, self.options.max_retry_delay_seconds)
        job.state = JobStates.QUEUED
        job.next_attempt_at = now + timedelta(seconds=delay)
        await self._store.update(job)
        logger.warning("Job %s (%s) failed on attempt %s; retrying in %ss: %s", job.id, job.type, job.attempts, delay, error)
        return telemetry.RETRYING_OUTCOME
