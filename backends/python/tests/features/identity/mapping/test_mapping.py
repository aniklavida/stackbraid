from datetime import datetime, timezone

from app.features.identity.application.mapping import role_to_dto, user_to_dto
from app.features.identity.domain.entities import Role, User

NOW = datetime(2026, 1, 1, tzinfo=timezone.utc)


def test_role_to_dto_carries_every_field() -> None:
    role = Role.create("admin", "Full access", ["users:read", "users:write"])
    role.created_at = role.updated_at = NOW

    dto = role_to_dto(role)

    assert dto.id == role.id
    assert dto.name == "admin"
    assert dto.description == "Full access"
    assert dto.permissions == ["users:read", "users:write"]


def test_user_to_dto_serializes_timestamps_with_trailing_z() -> None:
    from app.features.identity.domain.value_objects import Email

    user = User.register(Email.create("ada@example.com"), "hash", "Ada", NOW)
    user.created_at = user.updated_at = NOW

    dto = user_to_dto(user)
    payload = dto.model_dump(by_alias=True, mode="json")

    assert payload["createdAt"] == "2026-01-01T00:00:00Z"
    assert payload["email"] == "ada@example.com"
    assert payload["displayName"] == "Ada"
    assert payload["status"] == "active"
    assert payload["roles"] == []
