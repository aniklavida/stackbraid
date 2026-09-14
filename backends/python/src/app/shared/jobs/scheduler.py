"""Runs each queued job one at a time, in the background, logging failures
instead of crashing the process — the same in-process stand-in as
``InProcessMessagePublisher``, for fire-and-forget work such as sending a
welcome email after registration.
"""

from __future__ import annotations

import asyncio
import logging
from typing import Awaitable, Callable, Protocol

logger = logging.getLogger("stackbraid.jobs")

Job = Callable[[], Awaitable[None]]


class JobScheduler(Protocol):
    def enqueue(self, job: Job) -> None: ...


class InProcessJobScheduler:
    def __init__(self) -> None:
        self._queue: asyncio.Queue[Job] = asyncio.Queue()

    def enqueue(self, job: Job) -> None:
        self._queue.put_nowait(job)

    async def run_forever(self) -> None:
        while True:
            job = await self._queue.get()
            try:
                await job()
            except Exception:  # noqa: BLE001 - a failed background job must never crash the process
                logger.exception("Background job failed.")
