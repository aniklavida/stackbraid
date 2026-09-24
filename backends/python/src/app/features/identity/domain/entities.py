"""An account and its supporting entities. Each owns its own invariants — a
caller cannot put a ``User`` into a state the contract forbids (assigning a
role twice, deactivating twice) because the methods below are the only way
to change one. Depends on nothing outside this module and the standard
library: no Shared, no SQLAlchemy, no Pydantic — see ``value_objects.py``
and ``events.py``, the only other things this layer imports.
"""

from __future__ import annotations

from datetime import datetime
from enum import Enum
from typing import Sequence
from uuid import UUID, uuid4

from app.features.identity.domain.events import (
    DomainEvent,
    UserDeactivatedEvent,
    UserLoggedInEvent,
    UserRegisteredEvent,
    UserRoleAssignedEvent,
    UserRoleRevokedEvent,
)
from app.features.identity.domain.value_objects import Email


class UserStatus(str, Enum):
    ACTIVE = "active"
    INACTIVE = "inactive"


class Role:
    """A named set of permission codes. Roles are an administrator-curated
    catalogue, not user-generated data (see ``contract/openapi.yaml``'s
    ``/v1/roles`` — deliberately unpaginated) — the count stays small by
    design.
    """

    def __init__(self, id: UUID, name: str, description: str | None, permissions: Sequence[str]) -> None:
        self.id = id
        self.name = name
        self.description = description
        self.permissions: tuple[str, ...] = tuple(dict.fromkeys(permissions))
        self.created_at: datetime | None = None
        self.updated_at: datetime | None = None

    @staticmethod
    def create(name: str, description: str | None, permissions: Sequence[str]) -> "Role":
        if not name or not name.strip():
            raise ValueError("A role must have a name.")
        return Role(uuid4(), name, description, permissions)

    def has_permission(self, permission: str) -> bool:
        return permission in self.permissions


class UserRole:
    """The link between a ``User`` and a ``Role``. A plain join entity rather
    than an implicit many-to-many so ``assigned_at`` has somewhere to live —
    "when was this role granted" is a real audit question an admin screen
    will ask.
    """

    def __init__(self, user_id: UUID, role: Role, assigned_at: datetime) -> None:
        self.user_id = user_id
        self.role_id = role.id
        self.role = role
        self.assigned_at = assigned_at


class RefreshToken:
    """One issued refresh token. Only its hash is ever persisted — the raw
    value is returned to the caller once (in ``TokenPair.refreshToken``) and
    never stored, so a database read can never recover a usable token.
    Rotation is explicit: refreshing revokes the old token and records which
    new one replaced it, so a reused, already-rotated token is rejected.
    """

    def __init__(self, id: UUID, user_id: UUID, token_hash: str, expires_at: datetime, created_at: datetime) -> None:
        self.id = id
        self.user_id = user_id
        self.token_hash = token_hash
        self.expires_at = expires_at
        self.created_at = created_at
        self.revoked_at: datetime | None = None
        self.replaced_by_token_id: UUID | None = None

    @staticmethod
    def issue(user_id: UUID, token_hash: str, expires_at: datetime, now: datetime) -> "RefreshToken":
        return RefreshToken(uuid4(), user_id, token_hash, expires_at, now)

    def is_active(self, now: datetime) -> bool:
        return self.revoked_at is None and self.expires_at > now

    def revoke(self, now: datetime, replaced_by_token_id: UUID | None = None) -> None:
        if self.revoked_at is None:
            self.revoked_at = now
        if self.replaced_by_token_id is None:
            self.replaced_by_token_id = replaced_by_token_id


class User:
    """An account. See the module docstring — the methods below are the only
    way to change one.
    """

    def __init__(self, id: UUID, email: Email, password_hash: str, display_name: str) -> None:
        self.id = id
        self.email = email
        self.password_hash = password_hash
        self.display_name = display_name
        self.status = UserStatus.ACTIVE
        self.last_login_at: datetime | None = None
        self.deleted_at: datetime | None = None
        self.created_at: datetime | None = None
        self.updated_at: datetime | None = None
        self._user_roles: list[UserRole] = []
        self._domain_events: list[DomainEvent] = []

    @property
    def user_roles(self) -> Sequence[UserRole]:
        return tuple(self._user_roles)

    @property
    def roles(self) -> Sequence[Role]:
        return tuple(link.role for link in self._user_roles)

    @property
    def domain_events(self) -> Sequence[DomainEvent]:
        return tuple(self._domain_events)

    def clear_domain_events(self) -> None:
        self._domain_events.clear()

    def _raise(self, event: DomainEvent) -> None:
        self._domain_events.append(event)

    @staticmethod
    def register(email: Email, password_hash: str, display_name: str, now: datetime) -> "User":
        user = User(uuid4(), email, password_hash, display_name)
        user._raise(UserRegisteredEvent(user.id, email.value, now))
        return user

    def record_login(self, now: datetime) -> None:
        self.last_login_at = now
        self._raise(UserLoggedInEvent(self.id, now))

    def update_profile(self, display_name: str | None, email: Email | None) -> None:
        """Partial update — only the fields the caller actually supplied
        change (see ``UpdateUserRequest`` in the contract)."""

        if display_name is not None:
            self.display_name = display_name
        if email is not None:
            self.email = email

    def deactivate(self, now: datetime) -> None:
        """Idempotent — deactivating an already-inactive user is a no-op,
        per the contract's ``/v1/users/{userId}/deactivate``."""

        if self.status == UserStatus.INACTIVE:
            return
        self.status = UserStatus.INACTIVE
        self.deleted_at = now
        self._raise(UserDeactivatedEvent(self.id, now))

    def restore(self) -> None:
        if self.deleted_at is None:
            return
        self.deleted_at = None
        self.status = UserStatus.ACTIVE

    def has_role(self, role_id: UUID) -> bool:
        return any(link.role_id == role_id for link in self._user_roles)

    def assign_role(self, role: Role, now: datetime) -> None:
        """No-op if the user already holds this role — per the contract,
        assigning a held role returns the same state rather than erroring."""

        if self.has_role(role.id):
            return
        self._user_roles.append(UserRole(self.id, role, now))
        self._raise(UserRoleAssignedEvent(self.id, role.id, now))

    def revoke_role(self, role_id: UUID, now: datetime) -> None:
        """Revoking a role the user does not hold is treated as success —
        per the contract's ``DELETE /v1/users/{userId}/roles/{roleId}``."""

        link = next((ur for ur in self._user_roles if ur.role_id == role_id), None)
        if link is None:
            return
        self._user_roles.remove(link)
        self._raise(UserRoleRevokedEvent(self.id, role_id, now))
