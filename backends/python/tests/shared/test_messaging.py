from __future__ import annotations

from uuid import uuid4

from app.shared.messaging.broker import InMemoryMessageBus, MessageBusOptions, MessageEnvelope


def _bus(max_attempts: int, base_retry_seconds: float) -> InMemoryMessageBus:
    bus = InMemoryMessageBus(
        MessageBusOptions(max_attempts=max_attempts, base_retry_delay_seconds=base_retry_seconds, max_retry_delay_seconds=10.0)
    )

    async def no_sleep(delay: float) -> None:
        return None

    # Never actually sleep — the interval is asserted, not waited out.
    bus.delay_override = no_sleep
    return bus


async def test_a_handler_that_fails_then_succeeds_is_retried_with_exponential_backoff() -> None:
    attempts: list[int] = []
    bus = _bus(max_attempts=4, base_retry_seconds=0.05)

    async def handler(envelope: MessageEnvelope) -> None:
        attempts.append(envelope.delivery_attempt)
        if envelope.delivery_attempt < 3:
            raise RuntimeError("transient")

    bus.subscribe("orders.created", handler)
    await bus.publish("orders.created", '{"id":1}')
    await bus.deliver_pending()

    assert attempts == [1, 2, 3]
    assert bus.recorded_retry_delays == [0.05, 0.1]
    assert bus.dead_letters == []


async def test_a_handler_that_always_fails_lands_in_the_dead_letter_path() -> None:
    bus = _bus(max_attempts=2, base_retry_seconds=0.05)

    async def handler(envelope: MessageEnvelope) -> None:
        raise RuntimeError("permanent")

    bus.subscribe("orders.created", handler)
    await bus.publish("orders.created", '{"id":2}')
    await bus.deliver_pending()

    assert bus.recorded_retry_delays == [0.05]
    assert len(bus.dead_letters) == 1
    dead_letter = bus.dead_letters[0]
    assert dead_letter.routing_key == "orders.created"
    assert dead_letter.attempts == 2
    assert "permanent" in dead_letter.error


async def test_an_idempotent_handler_processes_a_redelivered_message_only_once() -> None:
    processed = []
    bus = _bus(max_attempts=3, base_retry_seconds=0.01)

    async def handler(envelope: MessageEnvelope) -> None:
        processed.append(envelope.message_id)

    bus.subscribe("orders.created", handler)
    envelope = MessageEnvelope(uuid4(), "orders.created", '{"id":3}')
    await bus.publish_envelope(envelope)
    await bus.deliver_pending()
    await bus.publish_envelope(envelope)
    await bus.deliver_pending()

    assert processed == [envelope.message_id]


async def test_a_retried_message_is_not_processed_again_when_the_broker_redelivers_it() -> None:
    attempts = 0
    bus = _bus(max_attempts=3, base_retry_seconds=0.01)

    async def handler(envelope: MessageEnvelope) -> None:
        nonlocal attempts
        attempts += 1
        if attempts == 1:
            raise RuntimeError("transient")

    bus.subscribe("orders.created", handler)
    envelope = MessageEnvelope(uuid4(), "orders.created", "{}")
    await bus.publish_envelope(envelope)
    await bus.deliver_pending()
    await bus.publish_envelope(envelope)
    await bus.deliver_pending()

    assert attempts == 2
    assert bus.dead_letters == []
