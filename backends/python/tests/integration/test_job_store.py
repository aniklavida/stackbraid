from __future__ import annotations

from sqlalchemy.ext.asyncio import async_sessionmaker

from app.shared.jobs.handlers import JobContext, JobHandlerRegistry, JobPayload
from app.shared.jobs.models import JobRequest, JobWorkerOptions
from app.shared.jobs.persistent import JobWorker, PersistentJobScheduler
from app.shared.jobs.sqlalchemy_store import SqlAlchemyJobStore


class RecordingHandler:
    def __init__(self, job_type: str) -> None:
        self.job_type = job_type
        self.attempts: list[int] = []

    async def handle(self, context: JobContext, payload: JobPayload) -> None:
        self.attempts.append(context.attempt)


async def test_a_job_persisted_in_postgres_is_found_by_a_new_scheduler_and_completes(engine) -> None:
    # Proves the restart-survival claim against the real store rather than the
    # in-memory one: a job written by one scheduler is found by a brand-new
    # scheduler built over the same Postgres, then completed by a worker.
    session_factory = async_sessionmaker(engine, expire_on_commit=False)
    store = SqlAlchemyJobStore(session_factory)
    handler = RecordingHandler("integration.restart.job")
    registry = JobHandlerRegistry([handler])

    before_restart = PersistentJobScheduler(store)
    job_id = await before_restart.enqueue_request(JobRequest(handler.job_type, '{"value":42}', "integration-owner", 3))

    after_restart = PersistentJobScheduler(store)
    persisted = await after_restart.status(job_id)
    assert persisted is not None
    assert persisted.status == "queued"
    assert persisted.owner_id == "integration-owner"

    worker = JobWorker(store, registry, JobWorkerOptions(base_retry_delay_seconds=0.02, max_retry_delay_seconds=1.0))
    await worker.run_due()

    completed = await after_restart.status(job_id)
    assert completed is not None
    assert completed.status == "succeeded"
    assert completed.attempts == 1
    assert handler.attempts == [1]
