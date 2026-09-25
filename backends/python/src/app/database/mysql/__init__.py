"""The MySQL provider package. Exposes the same small surface every provider
directory exposes, so the composition root can select one by name without
knowing which providers exist:

``create_engine`` · ``create_session_factory`` · ``seed`` ·
``create_job_store`` · ``resolve_dsn`` · ``ALEMBIC_INI`` · ``DSN_ENV_VAR``
"""

from __future__ import annotations

from pathlib import Path
from typing import TYPE_CHECKING

from app.database.mysql.engine import create_engine, create_session_factory
from app.database.mysql.seed import seed
from app.shared.jobs.sqlalchemy_store import SqlAlchemyJobStore

if TYPE_CHECKING:
    from app.host.config import Settings

ALEMBIC_INI = Path(__file__).resolve().parent / "alembic.ini"
DSN_ENV_VAR = "STACKBRAID_MYSQL_DSN"


def create_job_store(session_factory):
    return SqlAlchemyJobStore(session_factory)


def resolve_dsn(settings: "Settings") -> str:
    return settings.mysql_dsn
