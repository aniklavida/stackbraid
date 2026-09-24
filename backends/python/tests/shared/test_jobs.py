from __future__ import annotations

from datetime import datetime, timedelta, timezone

from app.shared.jobs.conformance import ConformanceDeadLetterJobHandler, ConformanceRetriesJobHandler
from app.shared.jobs.handlers import JobContext, JobHandlerRegistry, JobPayload
from app.shared.jobs.models import JobRequest, JobWorkerOptions
from app.shared.jobs.persistent import JobWorker, PersistentJobScheduler
from app.shared.jobs.store import InMemoryJobStore


class Clock:
    def __init__(self) -> None:
        self.now = datetime(2026, 1, 1, tzinfo=timezone.utc)

    def __call__(self) -> datetime:
        return self.now

    def advance(self, seconds: float) -> None:
        self.now += timedelta(seconds=seconds)


class RecordingHandler:
    def __init__(self, job_type: str, behavior=None) -> None:
        self.job_type = job_type
        self._behavior = behavior
        self.attempts: list[int] = []

    async def handle(self, context: JobContext, payload: JobPayload) -> None:
        self.attempts.append(context.attempt)
        if self._behavior is not None:
            await self._behavior(context)


async def test_queued_job_survives_scheduler_recreation_and_completes() -> None:
    # One store instance stands in for the persisted job store. Building a
    # second scheduler/worker over it — and dropping the first — is the process
    # restart: if the job lived only in the first scheduler's memory it would
    # be gone.
    store = InMemoryJobStore()
    clock = Clock()
    options = JobWorkerOptions(base_retry_delay_seconds=0.05, max_retry_delay_seconds=1.0)
    handler = RecordingHandler("restart.job")
    registry = JobHandlerRegistry([handler])

    before_restart = PersistentJobScheduler(store, now=clock)
    job_id = await before_restart.enqueue_request(JobRequest("restart.job", "{}", "owner-1", 3))

    after_restart = PersistentJobScheduler(store, now=clock)
    persisted = await after_restart.status(job_id)
    assert persisted is not None
    assert persisted.status == "queued"
    assert persisted.owner_id == "owner-1"

    worker = JobWorker(store, registry, options, now=clock)
    await worker.run_due()

    completed = await after_restart.status(job_id)
    assert completed is not None
    assert completed.status == "succeeded"
    assert handler.attempts == [1]


async def test_failed_job_retries_with_exponential_backoff_then_succeeds() -> None:
    store = InMemoryJobStore()
    clock = Clock()
    options = JobWorkerOptions(base_retry_delay_seconds=0.05, max_retry_delay_seconds=5.0)
    handler = RecordingHandler(ConformanceRetriesJobHandler.job_type, _retry_behavior)
    registry = JobHandlerRegistry([handler])
    scheduler = PersistentJobScheduler(store, now=clock)
    worker = JobWorker(store, registry, options, now=clock)

    job_id = await scheduler.enqueue_request(JobRequest(handler.job_type, "{}", "owner-2", 5))

    await worker.run_due()
    after_first = await scheduler.status(job_id)
    assert after_first is not None and after_first.attempts == 1 and after_first.status == "queued"

    # Not due yet — a pass before the backoff elapses must claim nothing.
    await worker.run_due()
    assert (await scheduler.status(job_id)).attempts == 1

    clock.advance(0.05)
    await worker.run_due()
    after_second = await scheduler.status(job_id)
    assert after_second is not None and after_second.attempts == 2 and after_second.status == "queued"

    clock.advance(0.10)
    await worker.run_due()
    completed = await scheduler.status(job_id)
    assert completed is not None and completed.status == "succeeded" and completed.attempts == 3
    assert handler.attempts == [1, 2, 3]


async def test_job_that_always_fails_lands_in_dead_letter_after_its_attempt_budget() -> None:
    store = InMemoryJobStore()
    clock = Clock()
    options = JobWorkerOptions(base_retry_delay_seconds=0.025, max_retry_delay_seconds=1.0)
    handler = RecordingHandler(ConformanceDeadLetterJobHandler.job_type, _always_fails)
    scheduler = PersistentJobScheduler(store, now=clock)
    worker = JobWorker(store, JobHandlerRegistry([handler]), options, now=clock)

    job_id = await scheduler.enqueue_request(JobRequest(handler.job_type, "{}", "owner-3", 2))

    await worker.run_due()
    assert (await scheduler.status(job_id)).status == "queued"

    clock.advance(0.025)
    await worker.run_due()

    dead_lettered = await scheduler.status(job_id)
    assert dead_lettered is not None
    assert dead_lettered.status == "dead-lettered"
    assert dead_lettered.attempts == 2
    assert "always fails" in (dead_lettered.last_error or "")
    assert handler.attempts == [1, 2]


async def _retry_behavior(context: JobContext) -> None:
    if context.attempt < ConformanceRetriesJobHandler.succeed_on_attempt:
        raise RuntimeError("not yet")


async def _always_fails(context: JobContext) -> None:
    raise RuntimeError("always fails")
