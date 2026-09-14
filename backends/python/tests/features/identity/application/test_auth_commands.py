import pytest

from app.features.identity.application.commands.auth_commands import (
    LoginCommand,
    LogoutCommand,
    RefreshTokenCommand,
    RegisterUserCommand,
)
from app.features.identity.domain.entities import User
from app.features.identity.domain.value_objects import Email
from tests.features.identity.application.fakes import (
    FakeAccessTokenIssuer,
    FakeRefreshTokenRepository,
    FakeUnitOfWork,
    FakeUserRepository,
    fake_password_hasher,
)


@pytest.mark.asyncio
async def test_register_creates_a_new_active_user() -> None:
    users = FakeUserRepository()
    command = RegisterUserCommand(users, fake_password_hasher(), FakeUnitOfWork())

    result = await command.handle("ada@example.com", "password123", "Ada Lovelace")

    assert result.is_success
    assert result.value.email == "ada@example.com"
    assert result.value.status == "active"


@pytest.mark.asyncio
async def test_register_rejects_a_duplicate_email() -> None:
    users = FakeUserRepository()
    hasher = fake_password_hasher()
    existing = User.register(Email.create("ada@example.com"), hasher.hash("x"), "Ada", __import__("datetime").datetime.now(__import__("datetime").timezone.utc))
    await users.add(existing)

    command = RegisterUserCommand(users, hasher, FakeUnitOfWork())
    result = await command.handle("ada@example.com", "password123", "Someone Else")

    assert not result.is_success
    assert result.error.code == "IDENTITY.EMAIL_ALREADY_REGISTERED"


@pytest.mark.asyncio
async def test_login_succeeds_with_correct_credentials_and_issues_a_refresh_token() -> None:
    users = FakeUserRepository()
    hasher = fake_password_hasher()
    register = RegisterUserCommand(users, hasher, FakeUnitOfWork())
    await register.handle("ada@example.com", "password123", "Ada")

    refresh_tokens = FakeRefreshTokenRepository()
    login = LoginCommand(users, refresh_tokens, hasher, FakeAccessTokenIssuer(), FakeUnitOfWork())

    result = await login.handle("ada@example.com", "password123")

    assert result.is_success
    assert result.value.access_token
    assert result.value.refresh_token
    assert len(refresh_tokens.tokens) == 1


@pytest.mark.asyncio
async def test_login_fails_with_the_generic_error_for_a_wrong_password() -> None:
    users = FakeUserRepository()
    hasher = fake_password_hasher()
    await RegisterUserCommand(users, hasher, FakeUnitOfWork()).handle("ada@example.com", "password123", "Ada")

    login = LoginCommand(users, FakeRefreshTokenRepository(), hasher, FakeAccessTokenIssuer(), FakeUnitOfWork())
    result = await login.handle("ada@example.com", "wrong-password")

    assert not result.is_success
    assert result.error.code == "IDENTITY.INVALID_CREDENTIALS"


@pytest.mark.asyncio
async def test_login_fails_with_the_same_generic_error_for_an_unknown_email() -> None:
    login = LoginCommand(FakeUserRepository(), FakeRefreshTokenRepository(), fake_password_hasher(), FakeAccessTokenIssuer(), FakeUnitOfWork())

    result = await login.handle("nobody@example.com", "whatever123")

    assert not result.is_success
    assert result.error.code == "IDENTITY.INVALID_CREDENTIALS"


@pytest.mark.asyncio
async def test_refresh_rotates_the_token_and_revokes_the_old_one() -> None:
    users = FakeUserRepository()
    hasher = fake_password_hasher()
    await RegisterUserCommand(users, hasher, FakeUnitOfWork()).handle("ada@example.com", "password123", "Ada")

    refresh_tokens = FakeRefreshTokenRepository()
    login_result = await LoginCommand(users, refresh_tokens, hasher, FakeAccessTokenIssuer(), FakeUnitOfWork()).handle(
        "ada@example.com", "password123"
    )
    old_raw_token = login_result.value.refresh_token

    refresh = RefreshTokenCommand(refresh_tokens, users, FakeAccessTokenIssuer(), FakeUnitOfWork())
    result = await refresh.handle(old_raw_token)

    assert result.is_success
    assert result.value.refresh_token != old_raw_token

    # The old token must now be rejected — it was rotated, not merely copied.
    replay_result = await refresh.handle(old_raw_token)
    assert not replay_result.is_success
    assert replay_result.error.code == "IDENTITY.REFRESH_TOKEN_INVALID"


@pytest.mark.asyncio
async def test_logout_is_idempotent_for_an_unknown_token() -> None:
    logout = LogoutCommand(FakeRefreshTokenRepository(), FakeUnitOfWork())

    result = await logout.handle("a-token-nobody-issued")

    assert result.is_success
