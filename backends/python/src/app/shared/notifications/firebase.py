from __future__ import annotations

import asyncio
import json
import urllib.request

from app.shared.notifications.models import NotificationMessage
from app.shared.notifications.senders import FirebaseOptions


class FirebaseHttpTransport:
    def __init__(self, options: FirebaseOptions) -> None:
        self._options = options

    async def send(self, tokens: list[str], notification: NotificationMessage) -> None:
        if not tokens:
            return
        if not self._options.project_id or not self._options.access_token:
            return
        for token in tokens:
            payload = json.dumps({"message": {"token": token, "notification": {"title": notification.title, "body": notification.body}, "data": notification.data or {}}}).encode()
            request = urllib.request.Request(
                f"https://fcm.googleapis.com/v1/projects/{self._options.project_id}/messages:send",
                data=payload,
                headers={"Authorization": f"Bearer {self._options.access_token}", "Content-Type": "application/json"},
                method="POST",
            )
            await asyncio.to_thread(self._send, request)

    @staticmethod
    def _send(request: urllib.request.Request) -> None:
        with urllib.request.urlopen(request) as response:
            if response.status >= 300:
                raise RuntimeError("Firebase Cloud Messaging request failed.")
