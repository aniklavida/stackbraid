"""Ports the Application layer depends on but does not implement — the JWT
issuer needs signing configuration that belongs to the composition root, not
to a use case. Substituted with a fake in unit tests; the real implementation
lives in ``endpoints/security.py`` because it is also where the token is
verified on the way back in.
"""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from typing import Protocol
from uuid import UUID


@dataclass(frozen=True, slots=True)
class IssuedAccessToken:
    token: str
    expires_at: datetime


class AccessTokenIssuer(Protocol):
    def issue(self, user_id: UUID, roles: list[str], permissions: list[str]) -> IssuedAccessToken: ...
