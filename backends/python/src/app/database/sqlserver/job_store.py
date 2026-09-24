"""The SQL Server-specific job store.

SQLAlchemy's MSSQL dialect does not translate ``with_for_update(skip_locked=True)``
into a locking hint — it silently emits no clause at all — so the shared,
provider-agnostic claim query would let two workers claim the same job on this
provider. This subclass supplies the T-SQL equivalent, ``WITH (UPDLOCK,
READPAST, ROWLOCK)``, which is what a ``SKIP LOCKED`` claim means on SQL Server:
skip a row another transaction already holds, and lock the ones this one takes.
"""

from __future__ import annotations

from datetime import datetime

from sqlalchemy import or_, select

from app.shared.jobs.models import JobRecord, JobStates
from app.shared.jobs.sqlalchemy_store import JobModel, SqlAlchemyJobStore, _to_record


class SqlServerJobStore(SqlAlchemyJobStore):
    @staticmethod
    def claim_statement(max_count: int, now: datetime):
        """The atomic claim, exposed so its compiled T-SQL can be asserted
        without a live SQL Server — the hint is the whole point of this
        subclass, and a test that cannot see it proves nothing."""
        return (
            select(JobModel)
            .with_hint(JobModel, "WITH (UPDLOCK, READPAST, ROWLOCK)", "mssql")
            .where(
                JobModel.state == JobStates.QUEUED,
                or_(JobModel.next_attempt_at.is_(None), JobModel.next_attempt_at <= now),
            )
            .order_by(JobModel.created_at)
            .limit(max_count)
        )

    async def claim_due(self, max_count: int, now: datetime) -> list[JobRecord]:
        stmt = self.claim_statement(max_count, now)

        async with self._session_factory() as session:
            models = (await session.scalars(stmt)).all()
            for model in models:
                model.state = JobStates.RUNNING
                model.attempts = model.attempts + 1
                model.updated_at = now
                model.next_attempt_at = None
            await session.commit()
            return [_to_record(model) for model in models]
