"""A validated, normalized email address. Normalization is lower-casing
only — this domain treats ``Ada@Example.com`` and ``ada@example.com`` as the
same account, which is what "the email address is already registered" (see
``contract/openapi.yaml``'s ``/v1/auth/register``) actually means to a user.
"""

from __future__ import annotations

import re
from dataclasses import dataclass

_EMAIL_PATTERN = re.compile(r"^[^@\s]+@[^@\s]+\.[^@\s]+$")


@dataclass(frozen=True, slots=True)
class Email:
    value: str

    def __post_init__(self) -> None:
        if not _EMAIL_PATTERN.match(self.value):
            raise ValueError(f"'{self.value}' is not a valid email address.")

    @staticmethod
    def create(raw: str) -> "Email":
        if not raw or not raw.strip():
            raise ValueError("Email address cannot be empty.")
        return Email(raw.strip().lower())

    def __str__(self) -> str:
        return self.value
