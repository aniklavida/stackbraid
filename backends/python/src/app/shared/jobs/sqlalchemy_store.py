"""The persisted ``JobStore``: a queued job is a row in ``shared_jobs``, so it
is still there when the API process restarts. The provider-agnostic SQLAlchemy
types keep this module free of any Postgres-only construct except the
``SKIP LOCKED`` clause on the claim query, which the provider supports and
which is what stops two workers claiming the same job.
"""

from __future__ import annotations

import uuid
from datetime import datetime
from uuid import UUID

from sqlalchemy import DateTime, Integer, String, Text, Uuid, or_, select
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker
from sqlalchemy.orm import Mapped, mapped_column

from app.shared.jobs.models import JobRecord, JobStates
from app.shared.persistence.base import OrmBase

JobSessionFactory = async_sessionmaker[AsyncSession]


class JobModel(OrmBase):
    __tablename__ = "shared_jobs"

    id: Mapped[uuid.UUID] = mapped_column(Uuid, primary_key=True)
    type: Mapped[str] = mapped_column(String(200), nullable=False)
    payload: Mapped[str] = mapped_column(Text, nullable=False, default="{}")
    owner_id: Mapped[str | None] = mapped_column(String(200), nullable=True, index=True)
    state: Mapped[str] = mapped_column(String(20), nullable=False, index=True)
    attempts: Mapped[int] = mapped_column(Integer, nullable=False, default=0)
    max_attempts: Mapped[int] = mapped_column(Integer, nullable=False, default=3)
    last_error: Mapped[str | None] = mapped_column(Text, nullable=True)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    updated_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    next_attempt_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)


class SqlAlchemyJobStore:
    def __init__(self, session_factory: JobSessionFactory) -> None:
        self._session_factory = session_factory

    async def add(self, job: JobRecord) -> None:
        async with self._session_factory() as session:
            session.add(_to_model(job))
            await session.commit()

    async def get(self, job_id: UUID) -> JobRecord | None:
        async with self._session_factory() as session:
            model = await session.get(JobModel, job_id)
            return _to_record(model) if model is not None else None

    async def get_by_owner(self, owner_id: str) -> list[JobRecord]:
        async with self._session_factory() as session:
            result = await session.scalars(
                select(JobModel).where(JobModel.owner_id == owner_id).order_by(JobModel.created_at)
            )
            return [_to_record(model) for model in result.all()]

    async def claim_due(self, max_count: int, now: datetime) -> list[JobRecord]:
        stmt = (
            select(JobModel)
            .where(
                JobModel.state == JobStates.QUEUED,
                or_(JobModel.next_attempt_at.is_(None), JobModel.next_attempt_at <= now),
            )
            .order_by(JobModel.created_at)
            .limit(max_count)
            .with_for_update(skip_locked=True)
        )

        async with self._session_factory() as session:
            models = (await session.scalars(stmt)).all()
            for model in models:
                model.state = JobStates.RUNNING
                model.attempts = model.attempts + 1
                model.updated_at = now
                model.next_attempt_at = None
            await session.commit()
            return [_to_record(model) for model in models]

    async def update(self, job: JobRecord) -> None:
        async with self._session_factory() as session:
            model = await session.get(JobModel, job.id)
            if model is None:
                return
            model.state = job.state
            model.attempts = job.attempts
            model.last_error = job.last_error
            model.updated_at = job.updated_at
            model.next_attempt_at = job.next_attempt_at
            await session.commit()


def _to_model(job: JobRecord) -> JobModel:
    return JobModel(
        id=job.id,
        type=job.type,
        payload=job.payload_json,
        owner_id=job.owner_id,
        state=job.state,
        attempts=job.attempts,
        max_attempts=job.max_attempts,
        last_error=job.last_error,
        created_at=job.created_at,
        updated_at=job.updated_at,
        next_attempt_at=job.next_attempt_at,
    )


def _to_record(model: JobModel) -> JobRecord:
    return JobRecord(
        id=model.id,
        type=model.type,
        payload_json=model.payload,
        owner_id=model.owner_id,
        state=model.state,
        attempts=model.attempts,
        max_attempts=model.max_attempts,
        last_error=model.last_error,
        created_at=model.created_at,
        updated_at=model.updated_at,
        next_attempt_at=model.next_attempt_at,
    )
