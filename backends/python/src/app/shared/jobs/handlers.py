"""The handler side of the job contract: what a running job knows, how its
payload is read, and how a persisted type name is resolved back to code.
A handler is expected to be idempotent — the same job can be attempted more
than once — so ``context.job_id`` is the natural idempotency key.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from typing import Any, Protocol
from uuid import UUID


@dataclass(frozen=True, slots=True)
class JobContext:
    job_id: UUID
    job_type: str
    owner_id: str | None
    attempt: int


class JobPayload:
    def __init__(self, json_text: str) -> None:
        self._json = json_text

    @property
    def json(self) -> str:
        return self._json

    def deserialize(self, target_type: type[Any] | None = None) -> Any:
        return json.loads(self._json)


class JobHandler(Protocol):
    job_type: str

    async def handle(self, context: JobContext, payload: JobPayload) -> None: ...


class JobHandlerRegistry:
    def __init__(self, handlers: list[JobHandler]) -> None:
        self._handlers = {handler.job_type: handler for handler in handlers}

    def resolve(self, job_type: str) -> JobHandler | None:
        return self._handlers.get(job_type)
