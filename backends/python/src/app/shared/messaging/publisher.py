"""The default message publisher: an in-process, in-memory queue drained by a
background task. Nothing leaves this process and nothing survives a restart —
genuinely correct for a single-instance deployment, and an honest placeholder
rather than a broker this skeleton doesn't yet run. Swap this registration
for a RabbitMQ-backed implementation without touching a single caller.
"""

from __future__ import annotations

import asyncio
import logging
from typing import Any, Protocol

logger = logging.getLogger("stackbraid.messaging")


class MessagePublisher(Protocol):
    async def publish(self, message: Any) -> None: ...


class InProcessMessagePublisher:
    def __init__(self) -> None:
        self._queue: asyncio.Queue[Any] = asyncio.Queue()

    async def publish(self, message: Any) -> None:
        await self._queue.put(message)

    async def drain_forever(self) -> None:
        """Logs each delivery — the visible half of the stand-in broker."""

        while True:
            message = await self._queue.get()
            logger.info("Message delivered in-process: %s", type(message).__name__)
