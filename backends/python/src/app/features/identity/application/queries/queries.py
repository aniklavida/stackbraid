"""Read-only use cases — one handler class per contract operation, same
shape as the command handlers, resolved directly from the container.
"""

from __future__ import annotations

from uuid import UUID

from app.features.identity.application.mapping import role_to_dto, user_search_result_to_page_dto, user_to_dto
from app.features.identity.contracts.dtos import RoleDto, UserDto, UserPageDto
from app.features.identity.domain.repositories import RoleRepository, UserRepository, UserSearchQuery
from app.shared.caching.cache import Cache
from app.shared.web.errors import AppError
from app.shared.web.result import Result


class GetUserQuery:
    def __init__(self, users: UserRepository, cache: Cache | None = None) -> None:
        self._users = users
        self._cache = cache

    async def handle(self, user_id: UUID, include_deleted: bool = False) -> Result[UserDto]:
        if not include_deleted and self._cache is not None:
            async def load() -> UserDto | None:
                cached_user = await self._users.get_by_id(user_id)
                return user_to_dto(cached_user) if cached_user else None

            user = await self._cache.get_or_create(f"identity:user:{user_id}", load, 300)
            if user is None:
                return Result.failure(AppError.not_found("IDENTITY.USER_NOT_FOUND", "identity.user_not_found"))
            return Result.success(user)

        user = await self._users.get_by_id(user_id, include_deleted=include_deleted)
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
