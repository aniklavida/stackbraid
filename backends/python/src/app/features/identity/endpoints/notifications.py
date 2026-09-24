from __future__ import annotations

from datetime import timezone
from uuid import UUID

from fastapi import APIRouter, Depends, Query, Request, Response
from fastapi.responses import JSONResponse

from app.features.identity.contracts.notifications import RefreshDeviceTokenRequest, RegisterDeviceTokenRequest
from app.features.identity.endpoints.dependencies import get_device_token_store, get_localizer, get_notification_store
from app.features.identity.endpoints.security import get_current_user_id
from app.shared.localization.localizer import AppLocalizer
from app.shared.notifications.models import DeviceTokenStore, NotificationStore
from app.shared.web.errors import AppError
from app.shared.web.problem import problem_response

router = APIRouter(tags=["Devices", "Notifications"])


def _timestamp(value):
    return value.astimezone(timezone.utc).isoformat().replace("+00:00", "Z")


def _device_response(device) -> dict:
    return {
        "id": str(device.id),
        "platform": device.platform,
        "appVersion": device.app_version,
        "registeredAt": _timestamp(device.registered_at),
        "updatedAt": _timestamp(device.updated_at),
    }


@router.post("/v1/devices", status_code=201)
async def register_device(
    body: RegisterDeviceTokenRequest,
    http_request: Request,
    user_id: UUID = Depends(get_current_user_id),
    store: DeviceTokenStore = Depends(get_device_token_store),
    localizer: AppLocalizer = Depends(get_localizer),
):
    try:
        device = await store.register(user_id, body.token, body.platform, body.app_version or "")
    except ValueError:
        error = AppError.conflict("DEVICE.TOKEN_ALREADY_REGISTERED", "notifications.token_already_registered")
        return problem_response(error, localizer, http_request)
    return JSONResponse(status_code=201, content=_device_response(device))


@router.patch("/v1/devices/{device_id}")
async def refresh_device(
    device_id: UUID,
    body: RefreshDeviceTokenRequest,
    http_request: Request,
    user_id: UUID = Depends(get_current_user_id),
    store: DeviceTokenStore = Depends(get_device_token_store),
    localizer: AppLocalizer = Depends(get_localizer),
):
    device = await store.refresh(user_id, device_id, body.token)
    if device is None:
        error = AppError.not_found("DEVICE.NOT_FOUND", "notifications.device_not_found")
        return problem_response(error, localizer, http_request)
    return JSONResponse(content=_device_response(device))


@router.delete("/v1/devices/{device_id}", status_code=204)
async def revoke_device(
    device_id: UUID,
    http_request: Request,
    user_id: UUID = Depends(get_current_user_id),
    store: DeviceTokenStore = Depends(get_device_token_store),
    localizer: AppLocalizer = Depends(get_localizer),
):
    if not await store.revoke(user_id, device_id):
        error = AppError.not_found("DEVICE.NOT_FOUND", "notifications.device_not_found")
        return problem_response(error, localizer, http_request)
    return Response(status_code=204)


@router.get("/v1/notifications")
async def list_notifications(
    http_request: Request,
    page: int = Query(default=1, ge=1),
    page_size: int = Query(default=20, alias="pageSize", ge=1, le=100),
    user_id: UUID = Depends(get_current_user_id),
    store: NotificationStore = Depends(get_notification_store),
):
    total_items = await store.count(user_id)
    items = await store.list(user_id, page, page_size)
    return JSONResponse(
        content={
            "page": page,
            "pageSize": page_size,
            "totalItems": total_items,
            "totalPages": (total_items + page_size - 1) // page_size,
            "items": [
                {
                    "id": str(item.id),
                    "title": item.title,
                    "body": item.body,
                    "read": item.read,
                    "createdAt": _timestamp(item.created_at),
                }
                for item in items
            ],
        }
    )
