"""Delivers a `RealtimeMessage` to whichever WebSocket connections are
subscribed to a user's notification stream or a job's progress channel —
the native-WebSocket counterpart of .NET's `SignalRRealtimePublisher`
(`backends/dotnet/src/Host/Realtime/SignalRRealtimePublisher.cs`).

Two implementations, chosen once at startup by whether a Redis (or
Valkey — both speak the same wire protocol; which one deploys is not
decided by this code) connection string is configured:

- `InProcessRealtimePublisher` delivers only to clients connected to
  *this* process. Correct for one instance, and needs nothing running.
- `RedisRealtimePublisher` publishes through Redis; a background task
  (`subscribe_forever`, started once in `host/main.py`'s lifespan) is the
  other half — it subscribes to the same channel prefix and forwards every
  message into this process's own `ConnectionRegistry`, so a client
  connected to this instance receives a message published by any
  instance, including itself.

Neither implementation, nor the `RealtimePublisher` protocol callers
depend on, names Redis in its interface — only `RedisRealtimePublisher`'s
own body does.
"""

from __future__ import annotations

import asyncio
import logging
from typing import TYPE_CHECKING, Protocol
from uuid import UUID

from app.shared.realtime.messages import RealtimeMessage

if TYPE_CHECKING:
    from fastapi import WebSocket
    from redis.asyncio import Redis

logger = logging.getLogger("stackbraid.realtime")

REDIS_CHANNEL_PREFIX = "stackbraid-realtime:"


def user_channel(user_id: UUID | str) -> str:
    return f"user:{user_id}"


def job_channel(job_id: UUID | str) -> str:
    return f"job:{job_id}"


class ConnectionRegistry:
    """Every WebSocket connected to *this* process, grouped by channel name
    (`user:{userId}` or `job:{jobId}`) — the hand-rolled analogue of a
    SignalR group; FastAPI's native WebSocket support has no group concept
    of its own.
    """

    def __init__(self) -> None:
        self._connections: dict[str, set[WebSocket]] = {}

    def add(self, channel: str, websocket: WebSocket) -> None:
        self._connections.setdefault(channel, set()).add(websocket)

    def remove(self, channel: str, websocket: WebSocket) -> None:
        sockets = self._connections.get(channel)
        if not sockets:
            return
        sockets.discard(websocket)
        if not sockets:
            self._connections.pop(channel, None)

    async def broadcast_local(self, channel: str, payload: str) -> None:
        for websocket in list(self._connections.get(channel, ())):
            try:
                await websocket.send_text(payload)
            except Exception:  # noqa: BLE001 - one dead connection must never break delivery to the rest
                logger.exception("Failed to deliver a realtime message to one connection; continuing.")


class RealtimePublisher(Protocol):
    async def publish_to_user(self, user_id: UUID, message: RealtimeMessage) -> None: ...

    async def publish_to_job(self, job_id: UUID, message: RealtimeMessage) -> None: ...


class InProcessRealtimePublisher:
    def __init__(self, registry: ConnectionRegistry) -> None:
        self._registry = registry

    async def publish_to_user(self, user_id: UUID, message: RealtimeMessage) -> None:
        await self._registry.broadcast_local(user_channel(user_id), message.model_dump_json(by_alias=True))

    async def publish_to_job(self, job_id: UUID, message: RealtimeMessage) -> None:
        await self._registry.broadcast_local(job_channel(job_id), message.model_dump_json(by_alias=True))


class RedisRealtimePublisher:
    def __init__(self, redis_client: Redis, registry: ConnectionRegistry) -> None:
        self._redis = redis_client
        self._registry = registry

    async def publish_to_user(self, user_id: UUID, message: RealtimeMessage) -> None:
        await self._redis.publish(REDIS_CHANNEL_PREFIX + user_channel(user_id), message.model_dump_json(by_alias=True))

    async def publish_to_job(self, job_id: UUID, message: RealtimeMessage) -> None:
        await self._redis.publish(REDIS_CHANNEL_PREFIX + job_channel(job_id), message.model_dump_json(by_alias=True))

    async def subscribe_forever(self) -> None:
        """Started once as a background task when Redis is configured —
        the other half of every `publish_to_*` call above. Runs until
        cancelled at shutdown.
        """
        pubsub = self._redis.pubsub()
        await pubsub.psubscribe(REDIS_CHANNEL_PREFIX + "*")
        try:
            async for item in pubsub.listen():
                if item.get("type") != "pmessage":
                    continue
                channel = item["channel"]
                if isinstance(channel, bytes):
                    channel = channel.decode("utf-8")
                data = item["data"]
                if isinstance(data, bytes):
                    data = data.decode("utf-8")
                await self._registry.broadcast_local(channel.removeprefix(REDIS_CHANNEL_PREFIX), data)
        except asyncio.CancelledError:
            raise
        finally:
            await pubsub.aclose()
