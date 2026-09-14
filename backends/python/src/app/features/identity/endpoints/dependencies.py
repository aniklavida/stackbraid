"""Resolves every command/query handler for a request directly — no
container framework, no mediator. FastAPI's own ``Depends`` graph is the
resolution mechanism, reading the singletons ``host/main.py`` placed on
``app.state`` at startup. This lives in ``endpoints`` (the delivery layer),
never in ``host``: ``host`` mounts these routers, so the reverse import
would be circular, and it is also the correct dependency direction — a
delivery layer wiring itself from persistence and application is normal,
Host only supplies the singletons it cannot construct per-request.
"""

from __future__ import annotations

from typing import AsyncIterator

from fastapi import Depends, Request
from sqlalchemy.ext.asyncio import AsyncSession

from app.features.identity.application.abstractions import AccessTokenIssuer
from app.features.identity.application.commands.auth_commands import (
    LoginCommand,
    LogoutCommand,
    RefreshTokenCommand,
    RegisterUserCommand,
)
from app.features.identity.application.commands.user_commands import (
    AssignRoleCommand,
    DeactivateUserCommand,
    RevokeRoleCommand,
    UpdateUserCommand,
)
from app.features.identity.application.queries.queries import GetUserQuery, ListRolesQuery, ListUsersQuery
from app.features.identity.persistence.repositories import (
    SqlAlchemyRefreshTokenRepository,
    SqlAlchemyRoleRepository,
    SqlAlchemyUserRepository,
)
from app.shared.localization.localizer import AppLocalizer
from app.shared.persistence.unit_of_work import SqlAlchemyUnitOfWork, UnitOfWork
from app.shared.realtime.publisher import RealtimePublisher
from app.shared.security.password_hasher import PasswordHasher


async def get_session(request: Request) -> AsyncIterator[AsyncSession]:
    session_factory = request.app.state.session_factory
    async with session_factory() as session:
        yield session


def get_unit_of_work(session: AsyncSession = Depends(get_session)) -> UnitOfWork:
    return SqlAlchemyUnitOfWork(session)


def get_user_repository(session: AsyncSession = Depends(get_session)) -> SqlAlchemyUserRepository:
    return SqlAlchemyUserRepository(session)


def get_role_repository(session: AsyncSession = Depends(get_session)) -> SqlAlchemyRoleRepository:
    return SqlAlchemyRoleRepository(session)


def get_refresh_token_repository(session: AsyncSession = Depends(get_session)) -> SqlAlchemyRefreshTokenRepository:
    return SqlAlchemyRefreshTokenRepository(session)


def get_localizer(request: Request) -> AppLocalizer:
    return request.app.state.localizer  # type: ignore[no-any-return]


def get_password_hasher(request: Request) -> PasswordHasher:
    return request.app.state.password_hasher  # type: ignore[no-any-return]


def get_access_token_issuer(request: Request) -> AccessTokenIssuer:
    return request.app.state.access_token_issuer  # type: ignore[no-any-return]


def get_realtime_publisher(request: Request) -> RealtimePublisher:
    return request.app.state.realtime_publisher  # type: ignore[no-any-return]


def get_register_user_command(
    users: SqlAlchemyUserRepository = Depends(get_user_repository),
    password_hasher: PasswordHasher = Depends(get_password_hasher),
    unit_of_work: UnitOfWork = Depends(get_unit_of_work),
) -> RegisterUserCommand:
    return RegisterUserCommand(users, password_hasher, unit_of_work)


def get_login_command(
    users: SqlAlchemyUserRepository = Depends(get_user_repository),
    refresh_tokens: SqlAlchemyRefreshTokenRepository = Depends(get_refresh_token_repository),
    password_hasher: PasswordHasher = Depends(get_password_hasher),
    access_token_issuer: AccessTokenIssuer = Depends(get_access_token_issuer),
    unit_of_work: UnitOfWork = Depends(get_unit_of_work),
) -> LoginCommand:
    return LoginCommand(users, refresh_tokens, password_hasher, access_token_issuer, unit_of_work)


def get_refresh_token_command(
    refresh_tokens: SqlAlchemyRefreshTokenRepository = Depends(get_refresh_token_repository),
    users: SqlAlchemyUserRepository = Depends(get_user_repository),
    access_token_issuer: AccessTokenIssuer = Depends(get_access_token_issuer),
    unit_of_work: UnitOfWork = Depends(get_unit_of_work),
) -> RefreshTokenCommand:
    return RefreshTokenCommand(refresh_tokens, users, access_token_issuer, unit_of_work)


def get_logout_command(
    refresh_tokens: SqlAlchemyRefreshTokenRepository = Depends(get_refresh_token_repository),
    unit_of_work: UnitOfWork = Depends(get_unit_of_work),
) -> LogoutCommand:
    return LogoutCommand(refresh_tokens, unit_of_work)


def get_update_user_command(
    users: SqlAlchemyUserRepository = Depends(get_user_repository),
    unit_of_work: UnitOfWork = Depends(get_unit_of_work),
) -> UpdateUserCommand:
    return UpdateUserCommand(users, unit_of_work)


def get_deactivate_user_command(
    users: SqlAlchemyUserRepository = Depends(get_user_repository),
    unit_of_work: UnitOfWork = Depends(get_unit_of_work),
    realtime: RealtimePublisher = Depends(get_realtime_publisher),
) -> DeactivateUserCommand:
    return DeactivateUserCommand(users, unit_of_work, realtime)


def get_assign_role_command(
    users: SqlAlchemyUserRepository = Depends(get_user_repository),
    roles: SqlAlchemyRoleRepository = Depends(get_role_repository),
    unit_of_work: UnitOfWork = Depends(get_unit_of_work),
    realtime: RealtimePublisher = Depends(get_realtime_publisher),
) -> AssignRoleCommand:
    return AssignRoleCommand(users, roles, unit_of_work, realtime)


def get_revoke_role_command(
    users: SqlAlchemyUserRepository = Depends(get_user_repository),
    roles: SqlAlchemyRoleRepository = Depends(get_role_repository),
    unit_of_work: UnitOfWork = Depends(get_unit_of_work),
    realtime: RealtimePublisher = Depends(get_realtime_publisher),
) -> RevokeRoleCommand:
    return RevokeRoleCommand(users, roles, unit_of_work, realtime)


def get_get_user_query(users: SqlAlchemyUserRepository = Depends(get_user_repository)) -> GetUserQuery:
    return GetUserQuery(users)


def get_list_users_query(users: SqlAlchemyUserRepository = Depends(get_user_repository)) -> ListUsersQuery:
    return ListUsersQuery(users)


def get_list_roles_query(roles: SqlAlchemyRoleRepository = Depends(get_role_repository)) -> ListRolesQuery:
    return ListRolesQuery(roles)
