from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol


@dataclass(frozen=True, slots=True)
class EmailMessage:
    to: str
    subject: str
    html_body: str


class EmailSender(Protocol):
    async def send(self, message: EmailMessage) -> None: ...


class SmtpEmailSender:
    def __init__(self, host: str | None = None, port: int = 587, username: str | None = None, password: str | None = None, from_address: str = "no-reply@example.com") -> None:
        self.captured_messages: list[EmailMessage] = []

    async def send(self, message: EmailMessage) -> None:
        self.captured_messages.append(message)
