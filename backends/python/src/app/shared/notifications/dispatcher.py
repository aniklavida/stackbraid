from __future__ import annotations

import asyncio
from typing import Callable

from app.shared.jobs.scheduler import JobScheduler
from app.shared.notifications.models import NotificationMessage, NotificationSender


class NotificationDispatcher:
    def __init__(self, senders: list[NotificationSender]) -> None:
        self._senders = senders

    async def dispatch(self, notification: NotificationMessage) -> None:
        for sender in self._senders:
            await sender.send(notification)


class QueuedNotificationJob:
    def __init__(self, scheduler: JobScheduler, dispatcher: NotificationDispatcher) -> None:
        self._scheduler = scheduler
        self._dispatcher = dispatcher

    def enqueue(self, notification: NotificationMessage) -> None:
        self._scheduler.enqueue(lambda: self._dispatcher.dispatch(notification))
