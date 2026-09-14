"""Sends mail over SMTP when a host is configured; otherwise logs the message
instead of sending it, so a fresh clone works with zero setup.
"""

from __future__ import annotations

import logging
import smtplib
from dataclasses import dataclass
from email.message import EmailMessage as MimeEmailMessage
from typing import Protocol

logger = logging.getLogger("stackbraid.mailing")


@dataclass(frozen=True, slots=True)
class EmailMessage:
    to: str
    subject: str
    html_body: str


class EmailSender(Protocol):
    async def send(self, message: EmailMessage) -> None: ...


class SmtpEmailSender:
    def __init__(
        self,
        host: str | None = None,
        port: int = 587,
        username: str | None = None,
        password: str | None = None,
        from_address: str = "no-reply@example.com",
    ) -> None:
        self._host = host
        self._port = port
        self._username = username
        self._password = password
        self._from_address = from_address

    async def send(self, message: EmailMessage) -> None:
        if not self._host:
            logger.info("SMTP host not configured — logging email instead of sending. to=%s subject=%s", message.to, message.subject)
            return

        mime = MimeEmailMessage()
        mime["From"] = self._from_address
        mime["To"] = message.to
        mime["Subject"] = message.subject
        mime.set_content(message.html_body, subtype="html")

        with smtplib.SMTP(self._host, self._port) as client:
            client.starttls()
            if self._username:
                client.login(self._username, self._password or "")
            client.send_message(mime)
