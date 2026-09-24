from __future__ import annotations

import logging
from dataclasses import dataclass
from datetime import datetime, timezone
from html import escape
from typing import Protocol
from uuid import uuid4

from app.shared.mailing.email_sender import EmailMessage, EmailSender
from app.shared.notifications.models import DeviceTokenStore, NotificationMessage, NotificationSender, NotificationStore, StoredNotification

logger = logging.getLogger("stackbraid.notifications")


class FirebaseCloudMessagingTransport(Protocol):
    async def send(self, tokens: list[str], notification: NotificationMessage) -> None: ...


@dataclass(frozen=True, slots=True)
class FirebaseOptions:
    project_id: str = ""
    access_token: str = ""


class EmailNotificationSender:
    channel = "email"

    def __init__(self, email_sender: EmailSender) -> None:
        self._email_sender = email_sender

    async def send(self, notification: NotificationMessage) -> None:
        html = f"<h1>{escape(notification.title)}</h1><p>{escape(notification.body)}</p>"
        await self._email_sender.send(EmailMessage(notification.email, notification.title, html))


class InAppNotificationSender:
    channel = "in-app"

    def __init__(self, store: NotificationStore) -> None:
        self._store = store

    async def send(self, notification: NotificationMessage) -> None:
        await self._store.add(
            StoredNotification(uuid4(), notification.user_id, notification.title, notification.body, False, datetime.now(timezone.utc))
        )


class FirebaseNotificationSender:
    channel = "firebase"

    def __init__(self, transport: FirebaseCloudMessagingTransport, device_tokens: DeviceTokenStore, options: FirebaseOptions) -> None:
        self._transport = transport
        self._device_tokens = device_tokens
        self._options = options

    async def send(self, notification: NotificationMessage) -> None:
        if not self._options.project_id or not self._options.access_token:
            logger.warning("Firebase Cloud Messaging push is disabled: Firebase credentials are not configured. Email and in-app delivery remain active.")
            return
        tokens = await self._device_tokens.tokens_for(notification.user_id)
        await self._transport.send(tokens, notification)
