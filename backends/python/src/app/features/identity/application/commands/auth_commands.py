"""Register, login, refresh and logout — one handler class per contract
operation, each with its own ``handle``. Application resolves handlers
directly from the container, the same way FastAPI already resolves
everything else — there is no mediator library here, unlike the .NET
backend, which needs one to dispatch through its source-generated pipeline.
"""

from __future__ import annotations

from datetime import datetime, timedelta, timezone

from app.features.identity.application.abstractions import AccessTokenIssuer
from app.features.identity.application.mapping import token_pair_to_dto, user_to_dto
from app.features.identity.contracts.dtos import TokenPairDto, UserDto
from app.features.identity.domain.entities import RefreshToken, User, UserStatus
from app.features.identity.domain.repositories import RefreshTokenRepository, UserRepository
from app.features.identity.domain.value_objects import Email
from app.shared.persistence.unit_of_work import UnitOfWork
from app.shared.security.opaque_token import generate_raw_token, hash_token
from app.shared.security.password_hasher import PasswordHasher
from app.shared.web.errors import AppError
from app.shared.web.result import Result

REFRESH_TOKEN_LIFETIME = timedelta(days=30)


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _invalid_credentials() -> AppError:
    return AppError.unauthorized("IDENTITY.INVALID_CREDENTIALS", "identity.invalid_credentials")


def _refresh_token_invalid() -> AppError:
    return AppError.unauthorized("IDENTITY.REFRESH_TOKEN_INVALID", "identity.refresh_token_invalid")


class RegisterUserCommand:
    """One handler, one operation: hashes the password, creates the account,
    and returns it — ``User.register`` owns the actual domain invariant (an
    active account, one ``UserRegisteredEvent``). Field-shape validation
    (email format, password length, display-name length) already happened
    against ``RegisterRequest`` at the FastAPI boundary.
    """

    def __init__(self, users: UserRepository, password_hasher: PasswordHasher, unit_of_work: UnitOfWork) -> None:
        self._users = users
        self._password_hasher = password_hasher
        self._unit_of_work = unit_of_work

    async def handle(self, email: str, password: str, display_name: str) -> Result[UserDto]:
        parsed_email = Email.create(email)

        if await self._users.exists_by_email(parsed_email):
            return Result.failure(AppError.conflict("IDENTITY.EMAIL_ALREADY_REGISTERED", "identity.email_already_registered"))

        now = _utc_now()
        password_hash = self._password_hasher.hash(password)
        user = User.register(parsed_email, password_hash, display_name, now)
        user.created_at = now
        user.updated_at = now

        await self._users.add(user)
        await self._unit_of_work.commit()

        return Result.success(user_to_dto(user))


class LoginCommand:
    """Exchanges credentials for a token pair. Deliberately returns the same
    generic "invalid credentials" error whether the email is unknown, the
    password is wrong, or the account is deactivated — telling a caller
    which of those is true is a user-enumeration and account-status leak.
    """

    def __init__(
        self,
        users: UserRepository,
        refresh_tokens: RefreshTokenRepository,
        password_hasher: PasswordHasher,
        access_token_issuer: AccessTokenIssuer,
        unit_of_work: UnitOfWork,
    ) -> None:
        self._users = users
        self._refresh_tokens = refresh_tokens
        self._password_hasher = password_hasher
        self._access_token_issuer = access_token_issuer
        self._unit_of_work = unit_of_work

    async def handle(self, email: str, password: str) -> Result[TokenPairDto]:
        try:
            parsed_email = Email.create(email)
        except ValueError:
            return Result.failure(_invalid_credentials())

        user = await self._users.get_by_email(parsed_email)
        if user is None or user.status == UserStatus.INACTIVE or not self._password_hasher.verify(password, user.password_hash):
            return Result.failure(_invalid_credentials())

        now = _utc_now()
        user.record_login(now)
        user.updated_at = now

        access_token = self._access_token_issuer.issue(user.id, [role.name for role in user.roles], _permissions(user))
        raw_refresh_token = generate_raw_token()
        refresh_token = RefreshToken.issue(user.id, hash_token(raw_refresh_token), now + REFRESH_TOKEN_LIFETIME, now)

        await self._refresh_tokens.add(refresh_token)
        await self._users.save_changes(user)
        await self._unit_of_work.commit()

        return Result.success(token_pair_to_dto(access_token.token, raw_refresh_token, access_token.expires_at))


class RefreshTokenCommand:
    """Rotates a refresh token: the presented token is revoked (recording
    which token replaced it) and a new pair is issued. A missing, expired, or
    already-rotated token all produce the same 401 — see
    ``contract/openapi.yaml``'s ``/v1/auth/refresh``.
    """

    def __init__(
        self,
        refresh_tokens: RefreshTokenRepository,
        users: UserRepository,
        access_token_issuer: AccessTokenIssuer,
        unit_of_work: UnitOfWork,
    ) -> None:
        self._refresh_tokens = refresh_tokens
        self._users = users
        self._access_token_issuer = access_token_issuer
        self._unit_of_work = unit_of_work

    async def handle(self, raw_refresh_token: str) -> Result[TokenPairDto]:
        now = _utc_now()
        presented_hash = hash_token(raw_refresh_token)
        existing = await self._refresh_tokens.get_by_token_hash(presented_hash)

        if existing is None or not existing.is_active(now):
            return Result.failure(_refresh_token_invalid())

        user = await self._users.get_by_id(existing.user_id)
        if user is None or user.status == UserStatus.INACTIVE:
            return Result.failure(_refresh_token_invalid())

        raw_new_token = generate_raw_token()
        new_token = RefreshToken.issue(user.id, hash_token(raw_new_token), now + REFRESH_TOKEN_LIFETIME, now)
        existing.revoke(now, new_token.id)

        await self._refresh_tokens.add(new_token)
        await self._refresh_tokens.save_changes(existing)
        await self._unit_of_work.commit()

        access_token = self._access_token_issuer.issue(user.id, [role.name for role in user.roles], _permissions(user))

        return Result.success(token_pair_to_dto(access_token.token, raw_new_token, access_token.expires_at))


class LogoutCommand:
    """Revokes one session. Idempotent by design — logging out a token that
    is missing or already revoked is still success, per
    ``contract/openapi.yaml``'s ``/v1/auth/logout``.
    """

    def __init__(self, refresh_tokens: RefreshTokenRepository, unit_of_work: UnitOfWork) -> None:
        self._refresh_tokens = refresh_tokens
        self._unit_of_work = unit_of_work

    async def handle(self, raw_refresh_token: str) -> Result[None]:
        existing = await self._refresh_tokens.get_by_token_hash(hash_token(raw_refresh_token))

        if existing is not None:
            existing.revoke(_utc_now())
            await self._refresh_tokens.save_changes(existing)
            await self._unit_of_work.commit()

        return Result.success(None)


def _permissions(user: User) -> list[str]:
    permissions: set[str] = set()
    for role in user.roles:
        permissions.update(role.permissions)
    return sorted(permissions)
