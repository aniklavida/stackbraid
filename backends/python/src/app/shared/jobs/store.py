"""Where a queued job lives between being enqueued and being run. The
in-memory store is for tests and single-process development; the SQLAlchemy
store in ``sqlalchemy_store.py`` is the one a real deployment uses, and is
why a job survives an API restart.
"""

from __future__ import annotations

import asyncio
from dataclasses import replace
from datetime import datetime
from typing import Protocol
from uuid import UUID

from app.shared.jobs.models import JobRecord, JobStates


class JobStore(Protocol):
    async def add(self, job: JobRecord) -> None: ...

    async def get(self, job_id: UUID) -> JobRecord | None: ...

    async def get_by_owner(self, owner_id: str) -> list[JobRecord]: ...

    async def claim_due(self, max_count: int, now: datetime) -> list[JobRecord]: ...

    async def update(self, job: JobRecord) -> None: ...


class InMemoryJobStore:
    """Thread-safe, in-memory ``JobStore``. Sharing one instance across two
    scheduler/worker compositions is how a restart is simulated without a
    database — the data outlives the composition exactly as a persisted store
    would.
    """

    def __init__(self) -> None:
        self._jobs: dict[UUID, JobRecord] = {}
        self._lock = asyncio.Lock()

    async def add(self, job: JobRecord) -> None:
        async with self._lock:
            self._jobs[job.id] = replace(job)

    async def get(self, job_id: UUID) -> JobRecord | None:
        async with self._lock:
            record = self._jobs.get(job_id)
            return replace(record) if record is not None else None

    async def get_by_owner(self, owner_id: str) -> list[JobRecord]:
        async with self._lock:
            return [replace(job) for job in sorted(self._jobs.values(), key=lambda job: job.created_at) if job.owner_id == owner_id]

    async def claim_due(self, max_count: int, now: datetime) -> list[JobRecord]:
        async with self._lock:
            claimed = [
                job
                for job in sorted(self._jobs.values(), key=lambda job: job.created_at)
                if job.state == JobStates.QUEUED and (job.next_attempt_at is None or job.next_attempt_at <= now)
            ][:max_count]

            for job in claimed:
                job.state = JobStates.RUNNING
                job.attempts += 1
                job.updated_at = now
                job.next_attempt_at = None

            return [replace(job) for job in claimed]

    async def update(self, job: JobRecord) -> None:
        async with self._lock:
            self._jobs[job.id] = replace(job)
