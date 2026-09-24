from __future__ import annotations

from uuid import UUID

from app.features.identity.application.mapping import user_to_dto
from app.features.identity.contracts.dtos import UserDto
from app.features.identity.domain.repositories import UserRepository
from app.shared.caching.cache import Cache
from app.shared.persistence.unit_of_work import UnitOfWork
from app.shared.web.errors import AppError
from app.shared.web.result import Result


class RestoreUserCommand:
    def __init__(self, users: UserRepository, unit_of_work: UnitOfWork, cache: Cache | None = None) -> None:
        self._users = users
        self._unit_of_work = unit_of_work
        self._cache = cache

    async def handle(self, user_id: UUID) -> Result[UserDto]:
        user = await self._users.get_by_id(user_id, include_deleted=True)
        if user is None:
            return Result.failure(AppError.not_found("IDENTITY.USER_NOT_FOUND", "identity.user_not_found"))

        user.restore()
        await self._users.save_changes(user)
        await self._unit_of_work.commit()
        if self._cache is not None:
            self._cache.remove(f"identity:user:{user.id}")
        return Result.success(user_to_dto(user))
