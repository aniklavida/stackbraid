"""Hand-written mapping from a domain entity to its contract DTO — no
mapping library ships. Explicit and debuggable: a forgotten field fails a
test rather than silently vanishing. Covered by
``tests/features/identity/mapping``.
"""

from __future__ import annotations

from app.features.identity.contracts.dtos import RoleDto, TokenPairDto, UserDto, UserPageDto
from app.features.identity.domain.entities import Role, User
from app.features.identity.domain.repositories import UserSearchResult


def role_to_dto(role: Role) -> RoleDto:
    return RoleDto(id=role.id, name=role.name, description=role.description, permissions=list(role.permissions))


def user_to_dto(user: User) -> UserDto:
    return UserDto(
        id=user.id,
        email=user.email.value,
        display_name=user.display_name,
        status=user.status.value,
        roles=[role_to_dto(role) for role in user.roles],
        created_at=user.created_at,  # type: ignore[arg-type]
        updated_at=user.updated_at,  # type: ignore[arg-type]
        last_login_at=user.last_login_at,
    )


def user_search_result_to_page_dto(result: UserSearchResult, page: int, page_size: int) -> UserPageDto:
    total_pages = (result.total_count + page_size - 1) // page_size if page_size else 0
    return UserPageDto(
        page=page,
        page_size=page_size,
        total_items=result.total_count,
        total_pages=max(total_pages, 0),
        items=[user_to_dto(user) for user in result.items],
    )


def token_pair_to_dto(access_token: str, refresh_token: str, expires_at) -> TokenPairDto:  # noqa: ANN001
    return TokenPairDto(access_token=access_token, refresh_token=refresh_token, token_type="Bearer", expires_at=expires_at)
