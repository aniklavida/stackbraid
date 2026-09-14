"""Pydantic DTOs — what another feature, and every generated client, may see.
Field names mirror ``contract/openapi.yaml`` exactly (camelCase on the wire,
via ``CamelModel``). This module must never import this feature's own
``domain``/``application``/``persistence``/``endpoints`` — mapping from a
domain entity into one of these lives in ``application/mapping.py``.
"""

from __future__ import annotations

from uuid import UUID

from app.features.identity.contracts.camel import CamelModel
from app.features.identity.contracts.datetime_utils import UtcDateTime


class RoleDto(CamelModel):
    id: UUID
    name: str
    description: str | None = None
    permissions: list[str]


class UserDto(CamelModel):
    id: UUID
    email: str
    display_name: str
    status: str
    roles: list[RoleDto]
    created_at: UtcDateTime
    updated_at: UtcDateTime
    last_login_at: UtcDateTime | None = None


class UserPageDto(CamelModel):
    page: int
    page_size: int
    total_items: int
    total_pages: int
    items: list[UserDto]


class TokenPairDto(CamelModel):
    access_token: str
    refresh_token: str
    token_type: str = "Bearer"
    expires_at: UtcDateTime
