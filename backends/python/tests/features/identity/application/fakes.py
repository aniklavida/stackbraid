"""In-memory fakes for the Identity domain's repository protocols and the
application-layer ports — substituted for a real database in every
application-layer unit test, the same role Testcontainers-free fakes play in
the .NET test suite for its own command handlers.
"""

from __future__ import annotations

from datetime import datetime, timedelta, timezone
from uuid import UUID

from app.features.identity.application.abstractions import IssuedAccessToken
from app.features.identity.domain.entities import RefreshToken, Role, User
from app.features.identity.domain.repositories import UserSearchQuery, UserSearchResult
from app.features.identity.domain.value_objects import Email
from app.shared.realtime.messages import RealtimeMessage
from app.shared.security.password_hasher import Pbkdf2PasswordHasher


class FakeUserRepository:
    def __init__(self) -> None:
        self.users: dict[UUID, User] = {}

    async def get_by_id(self, id: UUID) -> User | None:
        return self.users.get(id)

    async def get_by_email(self, email: Email) -> User | None:
        return next((u for u in self.users.values() if u.email.value == email.value), None)

    async def exists_by_email(self, email: Email) -> bool:
        return any(u.email.value == email.value for u in self.users.values())

    async def add(self, user: User) -> None:
        self.users[user.id] = user

    async def save_changes(self, user: User) -> None:
        self.users[user.id] = user

    async def search(self, query: UserSearchQuery) -> UserSearchResult:
        items = list(self.users.values())
        return UserSearchResult(items=items, total_count=len(items))


class FakeRoleRepository:
    def __init__(self) -> None:
        self.roles: dict[UUID, Role] = {}

    async def get_by_id(self, id: UUID) -> Role | None:
        return self.roles.get(id)

    async def list_all(self):
        return list(self.roles.values())

    async def add(self, role: Role) -> None:
        self.roles[role.id] = role


class FakeRefreshTokenRepository:
    def __init__(self) -> None:
        self.tokens: dict[str, RefreshToken] = {}

    async def get_by_token_hash(self, token_hash: str) -> RefreshToken | None:
        return self.tokens.get(token_hash)

    async def add(self, token: RefreshToken) -> None:
        self.tokens[token.token_hash] = token

    async def save_changes(self, token: RefreshToken) -> None:
        self.tokens[token.token_hash] = token


class FakeUnitOfWork:
    def __init__(self) -> None:
        self.committed = False

    async def commit(self) -> None:
        self.committed = True

    async def rollback(self) -> None:
        pass


class FakeAccessTokenIssuer:
    def issue(self, user_id: UUID, roles: list[str], permissions: list[str]) -> IssuedAccessToken:
        return IssuedAccessToken(f"fake-token-for-{user_id}", datetime.now(timezone.utc) + timedelta(minutes=15))


def fake_password_hasher() -> Pbkdf2PasswordHasher:
    return Pbkdf2PasswordHasher()


class FakeRealtimePublisher:
    """Records every message published instead of delivering it anywhere —
    a command-handler test asserts against `.to_user`/`.to_job`, the same
    way it asserts against a fake repository's own in-memory state."""

    def __init__(self) -> None:
        self.to_user: list[tuple[UUID, RealtimeMessage]] = []
        self.to_job: list[tuple[UUID, RealtimeMessage]] = []

    async def publish_to_user(self, user_id: UUID, message: RealtimeMessage) -> None:
        self.to_user.append((user_id, message))

    async def publish_to_job(self, job_id: UUID, message: RealtimeMessage) -> None:
        self.to_job.append((job_id, message))
