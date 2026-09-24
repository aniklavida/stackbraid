"""Publish/subscribe over a broker, with the semantics a real broker must
provide: at-least-once delivery, per-handler retry with exponential backoff, a
dead-letter path once the attempt budget is spent, and idempotent handlers so
a redelivery of the same message is not processed twice.

``InMemoryMessageBus`` is the dependency-free fake every test runs against.
A RabbitMQ-backed implementation is a second registration behind the same
``MessageBus`` interface — no caller changes when it arrives. Nothing here
connects to a real broker, and selecting ``RabbitMq`` raises rather than
silently falling back.
"""

from __future__ import annotations

import asyncio
import logging
from dataclasses import dataclass, replace
from typing import Awaitable, Callable, Protocol
from uuid import UUID, uuid4

from app.shared.jobs.models import BackoffPolicy

logger = logging.getLogger("stackbraid.messaging")


@dataclass(frozen=True, slots=True)
class MessageEnvelope:
    message_id: UUID
    routing_key: str
    payload_json: str
    delivery_attempt: int = 1


@dataclass(frozen=True, slots=True)
class DeadLetter:
    message_id: UUID
    routing_key: str
    payload_json: str
    attempts: int
    error: str


@dataclass(frozen=True, slots=True)
class MessageBusOptions:
    max_attempts: int = 3
    base_retry_delay_seconds: float = 0.1
    max_retry_delay_seconds: float = 30.0
    provider: str = "InMemory"


MessageHandler = Callable[[MessageEnvelope], Awaitable[None]]


class MessageBus(Protocol):
    async def publish(self, routing_key: str, payload_json: str) -> None: ...

    async def publish_envelope(self, envelope: MessageEnvelope) -> None: ...

    def subscribe(self, routing_key: str, handler: MessageHandler, idempotent: bool = True) -> Callable[[], None]: ...


class InMemoryMessageBus:
    def __init__(self, options: MessageBusOptions | None = None) -> None:
        self._options = options or MessageBusOptions()
        self._subscribers: dict[str, list[tuple[UUID, MessageHandler, bool]]] = {}
        self._pending: list[MessageEnvelope] = []
        self._processed: set[str] = set()
        self._dead_letters: list[DeadLetter] = []
        self._retry_delays: list[float] = []
        # Tests replace this with a no-op so a run never actually sleeps.
        self.delay_override: Callable[[float], Awaitable[None]] | None = None

    async def publish(self, routing_key: str, payload_json: str) -> None:
        await self.publish_envelope(MessageEnvelope(uuid4(), routing_key, payload_json))

    async def publish_envelope(self, envelope: MessageEnvelope) -> None:
        self._pending.append(envelope)

    def subscribe(self, routing_key: str, handler: MessageHandler, idempotent: bool = True) -> Callable[[], None]:
        subscriber_id = uuid4()
        self._subscribers.setdefault(routing_key, []).append((subscriber_id, handler, idempotent))

        def unsubscribe() -> None:
            self._subscribers[routing_key] = [
                entry for entry in self._subscribers.get(routing_key, []) if entry[0] != subscriber_id
            ]

        return unsubscribe

    @property
    def dead_letters(self) -> list[DeadLetter]:
        return list(self._dead_letters)

    @property
    def recorded_retry_delays(self) -> list[float]:
        return list(self._retry_delays)

    async def deliver_pending(self) -> int:
        pending, self._pending = self._pending, []
        deliveries = 0
        for envelope in pending:
            subscribers = list(self._subscribers.get(envelope.routing_key, []))
            if not subscribers:
                self._dead_letter(envelope, 0, f"No subscriber is registered for routing key '{envelope.routing_key}'.")
                continue
            for _, handler, idempotent in subscribers:
                await self._deliver(envelope, handler, idempotent)
                deliveries += 1
        return deliveries

    async def _deliver(self, envelope: MessageEnvelope, handler: MessageHandler, idempotent: bool) -> None:
        dedupe_key = f"{envelope.routing_key}:{envelope.message_id}"
        if idempotent and dedupe_key in self._processed:
            logger.debug("Skipping already-processed message %s on %s.", envelope.message_id, envelope.routing_key)
            return

        attempt = max(envelope.delivery_attempt, 1)
        while True:
            try:
                await handler(replace(envelope, delivery_attempt=attempt))
                if idempotent:
                    self._processed.add(dedupe_key)
                return
            except Exception as exc:  # noqa: BLE001 - a failing handler is a retryable attempt
                if attempt >= self._options.max_attempts:
                    self._dead_letter(envelope, attempt, str(exc))
                    return
                delay = BackoffPolicy.compute(attempt, self._options.base_retry_delay_seconds, self._options.max_retry_delay_seconds)
                self._retry_delays.append(delay)
                logger.warning(
                    "Message %s on %s failed on attempt %s; retrying in %ss: %s",
                    envelope.message_id, envelope.routing_key, attempt, delay, exc,
                )
                if self.delay_override is not None:
                    await self.delay_override(delay)
                else:
                    await asyncio.sleep(delay)
                attempt += 1

    def _dead_letter(self, envelope: MessageEnvelope, attempts: int, error: str) -> None:
        self._dead_letters.append(DeadLetter(envelope.message_id, envelope.routing_key, envelope.payload_json, attempts, error))
        logger.error(
            "Message %s on %s was dead-lettered after %s attempt(s): %s",
            envelope.message_id, envelope.routing_key, attempts, error,
        )


def select_message_bus(options: MessageBusOptions) -> MessageBus:
    """The registration point a RabbitMQ transport would slot into. There is
    deliberately no RabbitMQ transport compiled into this build, so selecting
    it fails loudly with the manual steps instead of silently using the fake."""
    if options.provider.lower() == "rabbitmq":
        raise NotImplementedError(
            "The RabbitMQ message transport is not wired in this build. To verify it: install and start a "
            "broker (e.g. `brew install rabbitmq && brew services start rabbitmq`), point the RabbitMQ options "
            "at it, add the `aio-pika` client, implement MessageBus against a durable queue, then set "
            "Messaging:Provider=RabbitMq. Until then, InMemoryMessageBus is the only implementation."
        )
    return InMemoryMessageBus(options)
