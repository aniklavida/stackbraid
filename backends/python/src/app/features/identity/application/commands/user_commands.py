"""Profile update, deactivation and role assignment/revocation — one handler
class per contract operation, mirroring ``auth_commands.py``.
"""

from __future__ import annotations

from datetime import datetime, timezone
from uuid import UUID

from app.features.identity.application.mapping import user_to_dto
from app.features.identity.contracts.dtos import UserDto
from app.features.identity.domain.entities import Role, UserStatus
from app.features.identity.domain.repositories import RoleRepository, UserRepository
from app.features.identity.domain.value_objects import Email
from app.shared.persistence.unit_of_work import UnitOfWork
from app.shared.realtime.messages import RoleSummary, UserDeactivatedMessage, UserRoleChangedMessage
from app.shared.realtime.publisher import RealtimePublisher
from app.shared.web.errors import AppError
from app.shared.web.result import Result


def _role_to_summary(role: Role) -> RoleSummary:
    return RoleSummary(id=role.id, name=role.name, description=role.description, permissions=list(role.permissions))


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _user_not_found() -> AppError:
    return AppError.not_found("IDENTITY.USER_NOT_FOUND", "identity.user_not_found")


def _role_not_found() -> AppError:
    return AppError.not_found("IDENTITY.ROLE_NOT_FOUND", "identity.role_not_found")


class UpdateUserCommand:
    def __init__(self, users: UserRepository, unit_of_work: UnitOfWork) -> None:
        self._users = users
        self._unit_of_work = unit_of_work

    async def handle(self, user_id: UUID, display_name: str | None, email: str | None) -> Result[UserDto]:
        user = await self._users.get_by_id(user_id)
        if user is None:
            return Result.failure(_user_not_found())

        new_email: Email | None = None
        if email is not None:
            try:
                new_email = Email.create(email)
            except ValueError:
                return Result.failure(
                    AppError.validation(
                        "IDENTITY.VALIDATION_FAILED",
                        "identity.validation_failed",
                        {"email": ["validation.email.invalid"]},
                    )
                )

            if new_email.value != user.email.value and await self._users.exists_by_email(new_email):
                return Result.failure(AppError.conflict("IDENTITY.EMAIL_IN_USE", "identity.email_in_use"))

        user.update_profile(display_name, new_email)
        user.updated_at = _utc_now()
        await self._users.save_changes(user)
        await self._unit_of_work.commit()

        return Result.success(user_to_dto(user))


class DeactivateUserCommand:
    """Idempotent — deactivating an already-inactive user returns the
    current state, not an error."""

    def __init__(self, users: UserRepository, unit_of_work: UnitOfWork, realtime: RealtimePublisher) -> None:
        self._users = users
        self._unit_of_work = unit_of_work
        self._realtime = realtime

    async def handle(self, user_id: UUID) -> Result[UserDto]:
        user = await self._users.get_by_id(user_id)
        if user is None:
            return Result.failure(_user_not_found())

        was_already_inactive = user.status == UserStatus.INACTIVE
        user.deactivate(_utc_now())
        user.updated_at = _utc_now()
        await self._users.save_changes(user)
        await self._unit_of_work.commit()

        # Idempotent per the class summary above — only a real transition notifies.
        if not was_already_inactive:
            await self._realtime.publish_to_user(
                user.id, UserDeactivatedMessage(user_id=user.id, occurred_at=_utc_now())
            )

        return Result.success(user_to_dto(user))


class AssignRoleCommand:
    """No-op if the user already holds the role — ``User.assign_role`` owns
    that invariant."""

    def __init__(self, users: UserRepository, roles: RoleRepository, unit_of_work: UnitOfWork, realtime: RealtimePublisher) -> None:
        self._users = users
        self._roles = roles
        self._unit_of_work = unit_of_work
        self._realtime = realtime

    async def handle(self, user_id: UUID, role_id: UUID) -> Result[UserDto]:
        user = await self._users.get_by_id(user_id)
        if user is None:
            return Result.failure(_user_not_found())

        role = await self._roles.get_by_id(role_id)
        if role is None:
            return Result.failure(_role_not_found())

        user.assign_role(role, _utc_now())
        user.updated_at = _utc_now()
        await self._users.save_changes(user)
        await self._unit_of_work.commit()

        await self._realtime.publish_to_user(
            user.id,
            UserRoleChangedMessage(
                user_id=user.id,
                roles=[_role_to_summary(r) for r in user.roles],
                occurred_at=_utc_now(),
            ),
        )

        return Result.success(user_to_dto(user))


class RevokeRoleCommand:
    """Revoking a role the user does not hold is still success — see
    ``contract/openapi.yaml``'s ``DELETE /v1/users/{userId}/roles/{roleId}``.
    The user or the role not existing at all is the only 404 case.
    """

    def __init__(self, users: UserRepository, roles: RoleRepository, unit_of_work: UnitOfWork, realtime: RealtimePublisher) -> None:
        self._users = users
        self._roles = roles
        self._unit_of_work = unit_of_work
        self._realtime = realtime

    async def handle(self, user_id: UUID, role_id: UUID) -> Result[None]:
        user = await self._users.get_by_id(user_id)
        if user is None:
            return Result.failure(_user_not_found())

        role = await self._roles.get_by_id(role_id)
        if role is None:
            return Result.failure(_role_not_found())

        user.revoke_role(role_id, _utc_now())
        user.updated_at = _utc_now()
        await self._users.save_changes(user)
        await self._unit_of_work.commit()

        await self._realtime.publish_to_user(
            user.id,
            UserRoleChangedMessage(
                user_id=user.id,
                roles=[_role_to_summary(r) for r in user.roles],
                occurred_at=_utc_now(),
            ),
        )

        return Result.success(None)
