from datetime import datetime, timezone

import pytest

from app.features.identity.domain.entities import Role, User, UserStatus
from app.features.identity.domain.value_objects import Email

NOW = datetime(2026, 1, 1, tzinfo=timezone.utc)


def make_user() -> User:
    return User.register(Email.create("ada@example.com"), "hashed", "Ada Lovelace", NOW)


def test_register_raises_one_domain_event() -> None:
    user = make_user()

    assert user.status == UserStatus.ACTIVE
    assert len(user.domain_events) == 1


def test_deactivate_is_idempotent() -> None:
    user = make_user()
    user.deactivate(NOW)
    user.clear_domain_events()

    user.deactivate(NOW)  # second call must be a no-op

    assert user.status == UserStatus.INACTIVE
    assert len(user.domain_events) == 0


def test_assign_role_is_a_no_op_when_already_held() -> None:
    user = make_user()
    role = Role.create("admin", None, ["users:read"])
    user.assign_role(role, NOW)
    user.clear_domain_events()

    user.assign_role(role, NOW)  # second call must be a no-op

    assert len(user.roles) == 1
    assert len(user.domain_events) == 0


def test_revoke_role_the_user_does_not_hold_is_a_no_op() -> None:
    user = make_user()
    user.clear_domain_events()
    role = Role.create("admin", None, ["users:read"])

    user.revoke_role(role.id, NOW)  # never assigned

    assert len(user.roles) == 0
    assert len(user.domain_events) == 0


def test_update_profile_only_changes_supplied_fields() -> None:
    user = make_user()

    user.update_profile("New Name", None)

    assert user.display_name == "New Name"
    assert user.email.value == "ada@example.com"


def test_email_rejects_an_invalid_address() -> None:
    with pytest.raises(ValueError):
        Email.create("not-an-email")


def test_email_normalizes_case() -> None:
    assert Email.create("Ada@Example.COM").value == "ada@example.com"
