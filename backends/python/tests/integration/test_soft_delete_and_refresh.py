"""End-to-end repository/command regression tests for two behaviours that
only a real database can prove: deactivating an already-soft-deleted user is
idempotent (the row must be loaded with the soft-delete filter lifted), and a
rotated-out refresh token is rejected. Both run through the real SQLAlchemy
repositories, not the in-memory fakes, so they catch a mapping or filter bug
the application-layer unit tests cannot.
"""

from datetime import datetime, timedelta, timezone

from app.features.identity.application.commands.auth_commands import RefreshTokenCommand
from app.features.identity.application.commands.user_commands import DeactivateUserCommand
from app.features.identity.domain.entities import RefreshToken, User
from app.features.identity.domain.value_objects import Email
from app.features.identity.persistence.repositories import (
    SqlAlchemyRefreshTokenRepository,
    SqlAlchemyUserRepository,
)
from app.shared.persistence.unit_of_work import SqlAlchemyUnitOfWork
from app.shared.security.opaque_token import generate_raw_token, hash_token
from tests.features.identity.application.fakes import (
    FakeAccessTokenIssuer,
    FakeRealtimePublisher,
)

NOW = datetime.now(timezone.utc)


async def test_deactivating_twice_through_the_real_repository_is_idempotent(session) -> None:
    users = SqlAlchemyUserRepository(session)
    uow = SqlAlchemyUnitOfWork(session)

    user = User.register(Email.create("deactivate-twice@example.com"), "hash", "Twice", NOW)
    user.created_at = user.updated_at = NOW
    await users.add(user)
    await session.commit()

    command = DeactivateUserCommand(users, uow, FakeRealtimePublisher())
    first = await command.handle(user.id)
    second = await command.handle(user.id)

    assert first.is_success, first.error
    assert second.is_success, second.error
    assert second.value.status == "inactive"


async def test_a_rotated_out_refresh_token_is_rejected_by_the_real_repository(session) -> None:
    users = SqlAlchemyUserRepository(session)
    tokens = SqlAlchemyRefreshTokenRepository(session)
    uow = SqlAlchemyUnitOfWork(session)

    user = User.register(Email.create("rotator@example.com"), "hash", "Rotator", NOW)
    user.created_at = user.updated_at = NOW
    await users.add(user)
    raw = generate_raw_token()
    await tokens.add(RefreshToken.issue(user.id, hash_token(raw), NOW + timedelta(days=30), NOW))
    await session.commit()

    command = RefreshTokenCommand(tokens, users, FakeAccessTokenIssuer(), uow)
    rotated = await command.handle(raw)
    assert rotated.is_success, rotated.error

    reuse = await command.handle(raw)
    assert not reuse.is_success
    assert reuse.error.code == "IDENTITY.REFRESH_TOKEN_INVALID"
