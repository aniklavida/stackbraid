"""Domain events raised by ``User`` — recorded on the entity as it mutates
and drained by the persistence layer after a successful commit, never
before, so a subscriber never reacts to a change that was then rolled back.
"""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from uuid import UUID


@dataclass(frozen=True, slots=True)
class UserRegisteredEvent:
    user_id: UUID
    email: str
    occurred_at: datetime


@dataclass(frozen=True, slots=True)
class UserLoggedInEvent:
    user_id: UUID
    occurred_at: datetime


@dataclass(frozen=True, slots=True)
class UserDeactivatedEvent:
    user_id: UUID
    occurred_at: datetime


@dataclass(frozen=True, slots=True)
class UserRoleAssignedEvent:
    user_id: UUID
    role_id: UUID
    occurred_at: datetime


@dataclass(frozen=True, slots=True)
class UserRoleRevokedEvent:
    user_id: UUID
    role_id: UUID
    occurred_at: datetime


DomainEvent = (
    UserRegisteredEvent | UserLoggedInEvent | UserDeactivatedEvent | UserRoleAssignedEvent | UserRoleRevokedEvent
)
