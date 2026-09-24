"""The shapes a persisted job moves through — the Python twin of the .NET
backend's ``Shared/Jobs`` records. The state strings are the exact values both
backends' status endpoints return, so the conformance suite can assert the
same behaviour without knowing which backend it is talking to.
"""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from uuid import UUID


class JobStates:
    QUEUED = "queued"
    RUNNING = "running"
    SUCCEEDED = "succeeded"
    DEAD_LETTERED = "dead-lettered"


@dataclass(frozen=True, slots=True)
class JobRequest:
    """A durable unit of work: a handler type plus a JSON payload, never a
    closure, so it can be stored and re-read after a restart."""

    type: str
    payload_json: str = "{}"
    owner_id: str | None = None
    max_attempts: int = 3


@dataclass(slots=True)
class JobRecord:
    id: UUID
    type: str
    payload_json: str
    owner_id: str | None
    state: str
    attempts: int
    max_attempts: int
    last_error: str | None
    created_at: datetime
    updated_at: datetime
    next_attempt_at: datetime | None = None

    def to_status(self) -> "JobStatus":
        return JobStatus(
            id=self.id,
            type=self.type,
            status=self.state,
            attempts=self.attempts,
            max_attempts=self.max_attempts,
            last_error=self.last_error,
            created_at=self.created_at,
            updated_at=self.updated_at,
            owner_id=self.owner_id,
        )


@dataclass(frozen=True, slots=True)
class JobStatus:
    id: UUID
    type: str
    status: str
    attempts: int
    max_attempts: int
    last_error: str | None
    created_at: datetime
    updated_at: datetime
    owner_id: str | None

    def to_response(self) -> dict:
        """The caller-facing shape — everything but the owner, which is an
        access-control detail rather than part of the status."""
        return {
            "id": str(self.id),
            "type": self.type,
            "status": self.status,
            "attempts": self.attempts,
            "maxAttempts": self.max_attempts,
            "lastError": self.last_error,
            "createdAt": _timestamp(self.created_at),
            "updatedAt": _timestamp(self.updated_at),
        }


@dataclass(frozen=True, slots=True)
class JobWorkerOptions:
    batch_size: int = 10
    poll_interval_seconds: float = 1.0
    base_retry_delay_seconds: float = 1.0
    max_retry_delay_seconds: float = 300.0
    worker_enabled: bool = True
    conformance_enabled: bool = False


class BackoffPolicy:
    """Exponential backoff with a ceiling, deterministic (no jitter) so the
    same delay sequence is reproducible — attempt 1 waits ``base``, attempt 2
    ``2 × base``, capped at ``max``. Identical to the .NET policy."""

    @staticmethod
    def compute(attempt: int, base_seconds: float, max_seconds: float) -> float:
        exponent = min(max(attempt - 1, 0), 20)
        return min(base_seconds * (2**exponent), max_seconds)


def _timestamp(value: datetime) -> str:
    from datetime import timezone

    return value.astimezone(timezone.utc).isoformat().replace("+00:00", "Z")
