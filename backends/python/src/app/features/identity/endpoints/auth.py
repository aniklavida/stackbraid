"""``/v1/auth/*`` — register/login/refresh carry ``security: []`` in the
contract: public, no bearer token needed. Logout and ``/me`` require one.
"""

from __future__ import annotations

from uuid import UUID

from fastapi import APIRouter, Depends, Request, Response
from fastapi.responses import JSONResponse

from app.features.identity.application.commands.auth_commands import (
    LoginCommand,
    LogoutCommand,
    RefreshTokenCommand,
    RegisterUserCommand,
)
from app.features.identity.application.queries.queries import GetUserQuery
from app.features.identity.contracts.requests import LoginRequest, RefreshRequest, RegisterRequest
from app.features.identity.endpoints.dependencies import (
    get_get_user_query,
    get_localizer,
    get_login_command,
    get_logout_command,
    get_refresh_token_command,
    get_register_user_command,
)
from app.features.identity.endpoints.refresh_cookie import clear_refresh_cookie, read_refresh_cookie, set_refresh_cookie
from app.features.identity.endpoints.security import get_current_user_id
from app.shared.localization.localizer import AppLocalizer
from app.shared.web.errors import AppError
from app.shared.web.problem import problem_response
from app.shared.web.rate_limit import enforce_auth_rate_limit

router = APIRouter(prefix="/v1/auth", tags=["Auth"])


@router.post("/register", status_code=201, dependencies=[Depends(enforce_auth_rate_limit)])
async def register(
    request: RegisterRequest,
    http_request: Request,
    command: RegisterUserCommand = Depends(get_register_user_command),
    localizer: AppLocalizer = Depends(get_localizer),
):
    result = await command.handle(request.email, request.password, request.display_name)
    if not result.is_success:
        return problem_response(result.error, localizer, http_request)
    return JSONResponse(status_code=201, content=result.value.model_dump(by_alias=True, mode="json"))


@router.post("/login", dependencies=[Depends(enforce_auth_rate_limit)])
async def login(
    request: LoginRequest,
    http_request: Request,
    command: LoginCommand = Depends(get_login_command),
    localizer: AppLocalizer = Depends(get_localizer),
):
    result = await command.handle(request.email, request.password)
    if not result.is_success:
        return problem_response(result.error, localizer, http_request)

    response = JSONResponse(content=result.value.model_dump(by_alias=True, mode="json"))
    set_refresh_cookie(response, result.value.refresh_token)
    return response


@router.post("/refresh", dependencies=[Depends(enforce_auth_rate_limit)])
async def refresh(
    http_request: Request,
    request: RefreshRequest | None = None,
    command: RefreshTokenCommand = Depends(get_refresh_token_command),
    localizer: AppLocalizer = Depends(get_localizer),
):
    raw_token = (request.refresh_token if request else None) or read_refresh_cookie(http_request)
    if not raw_token:
        error = AppError.unauthorized("IDENTITY.REFRESH_TOKEN_INVALID", "identity.refresh_token_invalid")
        return problem_response(error, localizer, http_request)

    result = await command.handle(raw_token)
    if not result.is_success:
        return problem_response(result.error, localizer, http_request)

    response = JSONResponse(content=result.value.model_dump(by_alias=True, mode="json"))
    set_refresh_cookie(response, result.value.refresh_token)
    return response


@router.post("/logout", status_code=204)
async def logout(
    http_request: Request,
    request: RefreshRequest | None = None,
    _user_id: UUID = Depends(get_current_user_id),
    command: LogoutCommand = Depends(get_logout_command),
):
    raw_token = (request.refresh_token if request else None) or read_refresh_cookie(http_request)
    if raw_token:
        await command.handle(raw_token)

    response = Response(status_code=204)
    clear_refresh_cookie(response)
    return response


@router.get("/me")
async def get_current_user(
    http_request: Request,
    user_id: UUID = Depends(get_current_user_id),
    query: GetUserQuery = Depends(get_get_user_query),
    localizer: AppLocalizer = Depends(get_localizer),
):
    result = await query.handle(user_id)
    if not result.is_success:
        return problem_response(result.error, localizer, http_request)
    return JSONResponse(content=result.value.model_dump(by_alias=True, mode="json"))
