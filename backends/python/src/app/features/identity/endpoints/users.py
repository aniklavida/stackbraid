"""``/v1/users/*`` — every route here requires authentication plus the named
permission (``users:read`` or ``users:write``), per
``contract/openapi.yaml``'s per-endpoint 403 responses.
"""

from __future__ import annotations

import re
from uuid import UUID

from fastapi import APIRouter, Depends, Request
from fastapi.responses import JSONResponse

from app.features.identity.application.commands.user_commands import DeactivateUserCommand, UpdateUserCommand
from app.features.identity.application.queries.queries import GetUserQuery, ListUsersQuery
from app.features.identity.contracts.requests import UpdateUserRequest
from app.features.identity.domain.entities import UserStatus
from app.features.identity.domain.repositories import UserSearchQuery
from app.features.identity.endpoints.dependencies import (
    get_deactivate_user_command,
    get_get_user_query,
    get_list_users_query,
    get_localizer,
    get_update_user_command,
)
from app.features.identity.endpoints.security import require_permission
from app.shared.localization.localizer import AppLocalizer
from app.shared.web.errors import AppError
from app.shared.web.problem import problem_response

router = APIRouter(prefix="/v1/users", tags=["Users"])

_SORT_PATTERN = re.compile(r"^-?(email|displayName|createdAt|status)$")


@router.get("", dependencies=[Depends(require_permission("users:read"))])
async def list_users(
    http_request: Request,
    page: int = 1,
    pageSize: int = 20,  # noqa: N803 - the contract's own query parameter name
    sort: str = "-createdAt",
    search: str | None = None,
    status: str | None = None,
    roleId: UUID | None = None,  # noqa: N803
    query: ListUsersQuery = Depends(get_list_users_query),
    localizer: AppLocalizer = Depends(get_localizer),
):
    field_errors: dict[str, list[str]] = {}

    if page < 1:
        field_errors["page"] = ["validation.page.minimum"]
    if not (1 <= pageSize <= 100):
        field_errors["pageSize"] = ["validation.page_size.range"]
    if not _SORT_PATTERN.match(sort):
        field_errors["sort"] = ["validation.sort.invalid"]

    parsed_status: UserStatus | None = None
    if status:
        try:
            parsed_status = UserStatus(status.lower())
        except ValueError:
            field_errors["status"] = ["validation.status.invalid"]

    if field_errors:
        error = AppError.validation("IDENTITY.VALIDATION_FAILED", "identity.validation_failed", field_errors)
        return problem_response(error, localizer, http_request)

    result = await query.handle(UserSearchQuery(page, pageSize, sort, search, parsed_status, roleId))
    return JSONResponse(content=result.value.model_dump(by_alias=True, mode="json"))


@router.get("/{user_id}", dependencies=[Depends(require_permission("users:read"))])
async def get_user(
    user_id: UUID,
    http_request: Request,
    query: GetUserQuery = Depends(get_get_user_query),
    localizer: AppLocalizer = Depends(get_localizer),
):
    result = await query.handle(user_id)
    if not result.is_success:
        return problem_response(result.error, localizer, http_request)
    return JSONResponse(content=result.value.model_dump(by_alias=True, mode="json"))


@router.patch("/{user_id}", dependencies=[Depends(require_permission("users:write"))])
async def update_user(
    user_id: UUID,
    request: UpdateUserRequest,
    http_request: Request,
    command: UpdateUserCommand = Depends(get_update_user_command),
    localizer: AppLocalizer = Depends(get_localizer),
):
    result = await command.handle(user_id, request.display_name, request.email)
    if not result.is_success:
        return problem_response(result.error, localizer, http_request)
    return JSONResponse(content=result.value.model_dump(by_alias=True, mode="json"))


@router.post("/{user_id}/deactivate", dependencies=[Depends(require_permission("users:write"))])
async def deactivate_user(
    user_id: UUID,
    http_request: Request,
    command: DeactivateUserCommand = Depends(get_deactivate_user_command),
    localizer: AppLocalizer = Depends(get_localizer),
):
    result = await command.handle(user_id)
    if not result.is_success:
        return problem_response(result.error, localizer, http_request)
    return JSONResponse(content=result.value.model_dump(by_alias=True, mode="json"))
