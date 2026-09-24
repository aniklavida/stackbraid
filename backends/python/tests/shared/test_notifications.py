from __future__ import annotations

import logging
from uuid import uuid4

from app.shared.notifications.dispatcher import NotificationDispatcher, QueuedNotificationJob
from app.shared.notifications.models import NotificationMessage
from app.shared.notifications.senders import EmailNotificationSender, FirebaseNotificationSender, FirebaseOptions, InAppNotificationSender
from app.shared.notifications.stores import InMemoryDeviceTokenStore, InMemoryNotificationStore


class RecordingEmailSender:
    def __init__(self) -> None:
        self.messages = []

    async def send(self, message) -> None:
        self.messages.append(message)


class RecordingTransport:
    def __init__(self) -> None:
        self.calls = []

    async def send(self, tokens, notification) -> None:
        self.calls.append((tokens, notification))


class CapturingScheduler:
    def __init__(self) -> None:
        self.job = None

    def enqueue(self, job) -> None:
        self.job = job

    async def run(self) -> None:
        await self.job()


async def test_queued_notification_dispatches_to_all_senders_and_in_app_repository() -> None:
    email = RecordingEmailSender()
    transport = RecordingTransport()
    device_tokens = InMemoryDeviceTokenStore()
    user_id = uuid4()
    await device_tokens.register(user_id, "device-token", "ios", "1.0")
    store = InMemoryNotificationStore()
    dispatcher = NotificationDispatcher(
        [
            FirebaseNotificationSender(transport, device_tokens, FirebaseOptions("project", "token")),
            EmailNotificationSender(email),
            InAppNotificationSender(store),
        ]
    )
    scheduler = CapturingScheduler()
    job = QueuedNotificationJob(scheduler, dispatcher)
    notification = NotificationMessage(user_id, "person@example.com", "Job complete", "Your export is ready.")

    job.enqueue(notification)
    await scheduler.run()

    assert transport.calls[0][0] == ["device-token"]
    assert email.messages[0].to == "person@example.com"
    assert (await store.list(user_id, 1, 20))[0].title == "Job complete"


async def test_missing_firebase_configuration_logs_and_keeps_email_and_in_app_active(caplog) -> None:
    email = RecordingEmailSender()
    transport = RecordingTransport()
    store = InMemoryNotificationStore()
    notification = NotificationMessage(uuid4(), "person@example.com", "Title", "Body")
    caplog.set_level(logging.WARNING, logger="stackbraid.notifications")
    dispatcher = NotificationDispatcher(
        [
            FirebaseNotificationSender(transport, InMemoryDeviceTokenStore(), FirebaseOptions()),
            EmailNotificationSender(email),
            InAppNotificationSender(store),
        ]
    )

    await dispatcher.dispatch(notification)

    assert email.messages
    assert await store.list(notification.user_id, 1, 20)
    assert "push is disabled" in caplog.text
    assert transport.calls == []
