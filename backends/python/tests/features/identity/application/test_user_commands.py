from datetime import datetime, timezone

import pytest

from app.features.identity.application.commands.user_commands import (
    AssignRoleCommand,
    DeactivateUserCommand,
    RevokeRoleCommand,
    UpdateUserCommand,
)
from app.features.identity.domain.entities import Role, User
from app.features.identity.domain.value_objects import Email
from app.shared.realtime.messages import UserDeactivatedMessage, UserRoleChangedMessage
from tests.features.identity.application.fakes import (
    FakeRealtimePublisher,
    FakeRoleRepository,
    FakeUnitOfWork,
    FakeUserRepository,
)

NOW = datetime(2026, 1, 1, tzinfo=timezone.utc)


async def _seed_user(users: FakeUserRepository) -> User:
    user = User.register(Email.create("ada@example.com"), "hash", "Ada", NOW)
    user.created_at = user.updated_at = NOW
    await users.add(user)
    return user


@pytest.mark.asyncio
async def test_update_user_returns_not_found_for_a_missing_user() -> None:
    command = UpdateUserCommand(FakeUserRepository(), FakeUnitOfWork())

    result = await command.handle(User.register(Email.create("x@example.com"), "h", "X", NOW).id, "New Name", None)

    assert not result.is_success
    assert result.error.code == "IDENTITY.USER_NOT_FOUND"


@pytest.mark.asyncio
async def test_update_user_rejects_an_email_already_in_use() -> None:
    users = FakeUserRepository()
    user = await _seed_user(users)
    other = User.register(Email.create("other@example.com"), "hash", "Other", NOW)
    await users.add(other)

    command = UpdateUserCommand(users, FakeUnitOfWork())
    result = await command.handle(user.id, None, "other@example.com")

    assert not result.is_success
    assert result.error.code == "IDENTITY.EMAIL_IN_USE"


@pytest.mark.asyncio
async def test_deactivate_user_is_idempotent_and_notifies_only_on_the_real_transition() -> None:
    users = FakeUserRepository()
    user = await _seed_user(users)
    realtime = FakeRealtimePublisher()
    command = DeactivateUserCommand(users, FakeUnitOfWork(), realtime)

    first = await command.handle(user.id)
    second = await command.handle(user.id)

    assert first.is_success and second.is_success
    assert first.value.status == second.value.status == "inactive"
    assert len(realtime.to_user) == 1
    notified_user_id, message = realtime.to_user[0]
    assert notified_user_id == user.id
    assert isinstance(message, UserDeactivatedMessage)


@pytest.mark.asyncio
async def test_assign_role_returns_not_found_for_a_missing_role() -> None:
    users = FakeUserRepository()
    user = await _seed_user(users)
    command = AssignRoleCommand(users, FakeRoleRepository(), FakeUnitOfWork(), FakeRealtimePublisher())

    result = await command.handle(user.id, Role.create("ghost", None, []).id)

    assert not result.is_success
    assert result.error.code == "IDENTITY.ROLE_NOT_FOUND"


@pytest.mark.asyncio
async def test_assign_role_publishes_the_users_full_role_set() -> None:
    users = FakeUserRepository()
    user = await _seed_user(users)
    roles = FakeRoleRepository()
    role = Role.create("editor", None, ["users:read"])
    await roles.add(role)
    realtime = FakeRealtimePublisher()

    command = AssignRoleCommand(users, roles, FakeUnitOfWork(), realtime)
    result = await command.handle(user.id, role.id)

    assert result.is_success
    assert len(realtime.to_user) == 1
    notified_user_id, message = realtime.to_user[0]
    assert notified_user_id == user.id
    assert isinstance(message, UserRoleChangedMessage)
    assert any(r.id == role.id for r in message.roles)


@pytest.mark.asyncio
async def test_revoke_role_the_user_never_held_is_still_success() -> None:
    users = FakeUserRepository()
    user = await _seed_user(users)
    roles = FakeRoleRepository()
    role = Role.create("admin", None, ["users:read"])
    await roles.add(role)

    command = RevokeRoleCommand(users, roles, FakeUnitOfWork(), FakeRealtimePublisher())
    result = await command.handle(user.id, role.id)

    assert result.is_success


@pytest.mark.asyncio
async def test_revoke_role_publishes_the_users_remaining_role_set() -> None:
    users = FakeUserRepository()
    user = await _seed_user(users)
    roles = FakeRoleRepository()
    role = Role.create("admin", None, ["users:read"])
    await roles.add(role)
    user.assign_role(role, NOW)
    realtime = FakeRealtimePublisher()

    command = RevokeRoleCommand(users, roles, FakeUnitOfWork(), realtime)
    result = await command.handle(user.id, role.id)

    assert result.is_success
    assert len(realtime.to_user) == 1
    _, message = realtime.to_user[0]
    assert isinstance(message, UserRoleChangedMessage)
    assert all(r.id != role.id for r in message.roles)
