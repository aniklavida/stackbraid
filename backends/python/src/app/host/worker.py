"""A worker entry point with no HTTP surface: it drains the persisted job
queue for the same Postgres the API writes to. Run it with
``PYTHONPATH=src python -m app.host.worker``, pointing
``STACKBRAID_POSTGRES_DSN`` at the API's database; it shares the composition
root's settings and schema path, so it picks up jobs the API queued — including
any queued before this process started.
"""

from __future__ import annotations

import asyncio
import os
import subprocess
import sys
from pathlib import Path

from app.database.postgres.engine import create_engine, create_session_factory
from app.host.config import Settings
from app.shared.jobs.conformance import conformance_handlers
from app.shared.jobs.handlers import JobHandlerRegistry
from app.shared.jobs.models import JobWorkerOptions
from app.shared.jobs.persistent import JobWorker
from app.shared.jobs.sqlalchemy_store import SqlAlchemyJobStore
from app.shared.observability.logging_setup import configure_logging

_ALEMBIC_INI = Path(__file__).resolve().parents[3] / "alembic.ini"


def _run_migrations(dsn: str) -> None:
    subprocess.run(
        [sys.executable, "-m", "alembic", "-c", str(_ALEMBIC_INI), "upgrade", "head"],
        check=True,
        env={"STACKBRAID_POSTGRES_DSN": dsn, "PATH": os.environ.get("PATH", "")},
    )


async def run_worker() -> None:
    settings = Settings()
    configure_logging()
    engine = create_engine(settings.postgres_dsn)
    session_factory = create_session_factory(engine)

    try:
        if settings.run_migrations_on_startup:
            await asyncio.to_thread(_run_migrations, settings.postgres_dsn)

        options = JobWorkerOptions(
            poll_interval_seconds=settings.jobs_poll_interval_seconds,
            base_retry_delay_seconds=settings.jobs_base_retry_delay_seconds,
            max_retry_delay_seconds=settings.jobs_max_retry_delay_seconds,
            worker_enabled=True,
            conformance_enabled=settings.jobs_conformance_enabled,
        )
        handlers = conformance_handlers() if settings.jobs_conformance_enabled else []
        worker = JobWorker(SqlAlchemyJobStore(session_factory), JobHandlerRegistry(handlers), options)
        await worker.run_forever()
    finally:
        await engine.dispose()


def main() -> None:
    asyncio.run(run_worker())


if __name__ == "__main__":
    main()
