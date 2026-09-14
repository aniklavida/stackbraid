"""The read/write shape every feature repository implements — no SQL
assumption crosses this line (no query-builder type leaking through), which
is what keeps a future MongoDB repository able to implement the same
contract. See ``STRUCTURE.md``'s MongoDB-readiness rule.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol, Sequence
from uuid import UUID

from app.features.identity.domain.entities import RefreshToken, Role, User, UserStatus
from app.features.identity.domain.value_objects import Email


@dataclass(frozen=True, slots=True)
class UserSearchQuery:
    page: int
    page_size: int
    sort: str
    search: str | None
    status: UserStatus | None
    role_id: UUID | None


@dataclass(frozen=True, slots=True)
class UserSearchResult:
    items: Sequence[User]
    total_count: int


class UserRepository(Protocol):
    async def get_by_id(self, id: UUID) -> User | None: ...

    async def get_by_email(self, email: Email) -> User | None: ...

    async def exists_by_email(self, email: Email) -> bool: ...

    async def add(self, user: User) -> None: ...

    async def search(self, query: UserSearchQuery) -> UserSearchResult: ...


class RoleRepository(Protocol):
    async def get_by_id(self, id: UUID) -> Role | None: ...

    async def list_all(self) -> Sequence[Role]: ...

    async def add(self, role: Role) -> None: ...


class RefreshTokenRepository(Protocol):
    async def get_by_token_hash(self, token_hash: str) -> RefreshToken | None: ...

    async def add(self, token: RefreshToken) -> None: ...
