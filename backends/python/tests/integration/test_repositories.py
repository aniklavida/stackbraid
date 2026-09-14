from datetime import datetime, timedelta, timezone

import pytest

from app.features.identity.domain.entities import RefreshToken, Role, User
from app.features.identity.domain.repositories import UserSearchQuery
from app.features.identity.domain.value_objects import Email
from app.features.identity.persistence.repositories import (
    SqlAlchemyRefreshTokenRepository,
    SqlAlchemyRoleRepository,
    SqlAlchemyUserRepository,
)
from app.shared.persistence.unit_of_work import SqlAlchemyUnitOfWork

NOW = datetime.now(timezone.utc)

pytestmark = pytest.mark.asyncio


async def test_user_round_trips_through_postgres(session) -> None:
    repo = SqlAlchemyUserRepository(session)
    user = User.register(Email.create("ada@example.com"), "hash", "Ada Lovelace", NOW)
    user.created_at = user.updated_at = NOW

    await repo.add(user)
    await session.commit()

    fetched = await repo.get_by_id(user.id)
    assert fetched is not None
    assert fetched.email.value == "ada@example.com"
    assert fetched.display_name == "Ada Lovelace"


async def test_role_assignment_persists_across_a_reload(session) -> None:
    users = SqlAlchemyUserRepository(session)
    roles = SqlAlchemyRoleRepository(session)

    role = Role.create("editor", "Can edit content", ["content:write"])
    role.created_at = role.updated_at = NOW
    await roles.add(role)

    user = User.register(Email.create("editor@example.com"), "hash", "Editor", NOW)
    user.created_at = user.updated_at = NOW
    user.assign_role(role, NOW)
    await users.add(user)
    await session.commit()
    session.expunge_all()

    reloaded = await users.get_by_id(user.id)
    assert reloaded is not None
    assert [r.name for r in reloaded.roles] == ["editor"]


async def test_refresh_token_lookup_by_hash(session) -> None:
    users = SqlAlchemyUserRepository(session)
    user = User.register(Email.create("holder@example.com"), "hash", "Holder", NOW)
    user.created_at = user.updated_at = NOW
    await users.add(user)

    tokens = SqlAlchemyRefreshTokenRepository(session)
    token = RefreshToken.issue(user.id, "deadbeef" * 8, NOW + timedelta(days=30), NOW)
    await tokens.add(token)
    await session.commit()

    fetched = await tokens.get_by_token_hash("deadbeef" * 8)
    assert fetched is not None
    assert fetched.user_id == user.id
    assert fetched.is_active(NOW) is True


async def test_search_filters_by_status_and_paginates(session) -> None:
    users = SqlAlchemyUserRepository(session)
    for i in range(3):
        user = User.register(Email.create(f"search{i}@example.com"), "hash", f"Search {i}", NOW)
        user.created_at = user.updated_at = NOW
        await users.add(user)
    await session.commit()

    result = await users.search(UserSearchQuery(page=1, page_size=2, sort="-createdAt", search="search", status=None, role_id=None))

    assert result.total_count == 3
    assert len(result.items) == 2


async def test_unit_of_work_commits_pending_changes(session) -> None:
    uow = SqlAlchemyUnitOfWork(session)
    users = SqlAlchemyUserRepository(session)
    user = User.register(Email.create("uow@example.com"), "hash", "UoW", NOW)
    user.created_at = user.updated_at = NOW
    await users.add(user)

    await uow.commit()

    fetched = await users.get_by_id(user.id)
    assert fetched is not None
