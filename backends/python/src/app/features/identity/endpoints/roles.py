"""``/v1/roles`` and ``/v1/users/{userId}/roles*`` — ``roles:read`` /
``roles:write`` permission-gated, per ``contract/openapi.yaml``.
"""

from __future__ import annotations

from uuid import UUID

from fastapi import APIRouter, Depends, Request, Response
from fastapi.responses import JSONResponse

from app.features.identity.application.commands.user_commands import AssignRoleCommand, RevokeRoleCommand
from app.features.identity.application.queries.queries import ListRolesQuery
from app.features.identity.contracts.requests import AssignRoleRequest
from app.features.identity.endpoints.dependencies import (
    get_assign_role_command,
    get_list_roles_query,
    get_localizer,
    get_revoke_role_command,
)
from app.features.identity.endpoints.security import require_permission
from app.shared.localization.localizer import AppLocalizer
from app.shared.web.problem import problem_response

router = APIRouter(tags=["Roles"])


@router.get("/v1/roles", dependencies=[Depends(require_permission("roles:read"))])
async def list_roles(query: ListRolesQuery = Depends(get_list_roles_query)):
    result = await query.handle()
    return JSONResponse(content=[role.model_dump(by_alias=True, mode="json") for role in result.value])


@router.post("/v1/users/{user_id}/roles", dependencies=[Depends(require_permission("roles:write"))])
async def assign_role(
    user_id: UUID,
    request: AssignRoleRequest,
    http_request: Request,
    command: AssignRoleCommand = Depends(get_assign_role_command),
    localizer: AppLocalizer = Depends(get_localizer),
):
    result = await command.handle(user_id, request.role_id)
    if not result.is_success:
        return problem_response(result.error, localizer, http_request)
    return JSONResponse(content=result.value.model_dump(by_alias=True, mode="json"))


@router.delete("/v1/users/{user_id}/roles/{role_id}", status_code=204, dependencies=[Depends(require_permission("roles:write"))])
async def revoke_role(
    user_id: UUID,
    role_id: UUID,
    http_request: Request,
    command: RevokeRoleCommand = Depends(get_revoke_role_command),
    localizer: AppLocalizer = Depends(get_localizer),
):
    result = await command.handle(user_id, role_id)
    if not result.is_success:
        return problem_response(result.error, localizer, http_request)
    return Response(status_code=204)
