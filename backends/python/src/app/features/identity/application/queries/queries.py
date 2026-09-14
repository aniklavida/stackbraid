"""Read-only use cases — one handler class per contract operation, same
shape as the command handlers, resolved directly from the container.
"""

from __future__ import annotations

from uuid import UUID

from app.features.identity.application.mapping import role_to_dto, user_search_result_to_page_dto, user_to_dto
from app.features.identity.contracts.dtos import RoleDto, UserDto, UserPageDto
from app.features.identity.domain.repositories import RoleRepository, UserRepository, UserSearchQuery
from app.shared.web.errors import AppError
from app.shared.web.result import Result


class GetUserQuery:
    def __init__(self, users: UserRepository) -> None:
        self._users = users

    async def handle(self, user_id: UUID) -> Result[UserDto]:
        user = await self._users.get_by_id(user_id)
        if user is None:
            return Result.failure(AppError.not_found("IDENTITY.USER_NOT_FOUND", "identity.user_not_found"))
        return Result.success(user_to_dto(user))


class ListUsersQuery:
    def __init__(self, users: UserRepository) -> None:
        self._users = users

    async def handle(self, query: UserSearchQuery) -> Result[UserPageDto]:
        result = await self._users.search(query)
        return Result.success(user_search_result_to_page_dto(result, query.page, query.page_size))


class ListRolesQuery:
    def __init__(self, roles: RoleRepository) -> None:
        self._roles = roles

    async def handle(self) -> Result[list[RoleDto]]:
        roles = await self._roles.list_all()
        return Result.success([role_to_dto(role) for role in roles])
