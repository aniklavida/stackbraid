"""Native WebSocket endpoints — this backend's own transport for the
channels ``contract/openapi.yaml``'s ``x-realtime-channels`` section
describes. The .NET backend uses SignalR instead; the JSON either backend
pushes over its own transport is identical
(``app.shared.realtime.messages``).

A browser's ``WebSocket`` constructor cannot set an ``Authorization``
header, so both routes read the access token from a query parameter
instead — the same accommodation the contract's SignalR side documents.
"""

from __future__ import annotations

import asyncio
import json
import logging
from datetime import datetime, timezone
from uuid import UUID

from fastapi import APIRouter, WebSocket, WebSocketDisconnect

from app.features.identity.endpoints.security import AuthenticationError, JwtOptions, decode_access_token
from app.shared.jobs.scheduler import JobScheduler
from app.shared.realtime.messages import JobProgressMessage
from app.shared.realtime.publisher import ConnectionRegistry, RealtimePublisher, job_channel, user_channel

logger = logging.getLogger("stackbraid.realtime")

router = APIRouter()

_DEMO_JOB_FRAMES: list[tuple[str, int]] = [
    ("queued", 0),
    ("running", 40),
    ("running", 80),
    ("succeeded", 100),
]


def _user_id_from_query_token(websocket: WebSocket, jwt_options: JwtOptions) -> UUID:
    token = websocket.query_params.get("access_token", "")
    if not token:
        raise AuthenticationError("Missing access_token query parameter.")
    claims = decode_access_token(token, jwt_options)
    return UUID(claims["sub"])


@router.websocket("/v1/ws/notifications")
async def notifications_websocket(websocket: WebSocket) -> None:
    """Per-user stream carrying ``UserDeactivatedMessage`` and
    ``UserRoleChangedMessage`` — group ``user:{userId}`` on the .NET side,
    one connection per user here."""

    jwt_options: JwtOptions = websocket.app.state.jwt_options
    try:
        user_id = _user_id_from_query_token(websocket, jwt_options)
    except AuthenticationError:
        await websocket.close(code=4401)
        return

    registry: ConnectionRegistry = websocket.app.state.realtime_registry
    channel = user_channel(user_id)
    await websocket.accept()
    registry.add(channel, websocket)
    try:
        while True:
            # Server-to-client only; anything a client sends is ignored
            # rather than rejected, so an idle client's own keep-alive
            # frame never closes the connection.
            await websocket.receive_text()
    except WebSocketDisconnect:
        pass
    finally:
        registry.remove(channel, websocket)


@router.websocket("/v1/ws/jobs/{job_id}")
async def jobs_websocket(websocket: WebSocket, job_id: UUID) -> None:
    """Carries ``JobProgressMessage`` — group ``job:{jobId}`` on the .NET
    side. A client that sends ``{"action": "start_demo_job"}`` starts the
    same simulated four-frame job .NET's ``JobsHub.StartDemoJob`` starts —
    enough to prove a client watching this job id from a different
    connection (a second tab, or a second backend instance behind the same
    backplane) receives every frame. See ``docs/SPEC.md`` §13: the shipped
    realtime surface exists to prove the plumbing, not to be a job of its
    own.
    """

    jwt_options: JwtOptions = websocket.app.state.jwt_options
    try:
        _user_id_from_query_token(websocket, jwt_options)
    except AuthenticationError:
        await websocket.close(code=4401)
        return

    registry: ConnectionRegistry = websocket.app.state.realtime_registry
    channel = job_channel(job_id)
    await websocket.accept()
    registry.add(channel, websocket)
    try:
        while True:
            raw = await websocket.receive_text()
            await _handle_inbound(raw, job_id, websocket.app.state)
    except WebSocketDisconnect:
        pass
    finally:
        registry.remove(channel, websocket)


async def _handle_inbound(raw: str, job_id: UUID, state) -> None:
    try:
        payload = json.loads(raw)
    except ValueError:
        return

    if not isinstance(payload, dict) or payload.get("action") != "start_demo_job":
        return

    scheduler: JobScheduler = state.job_scheduler
    publisher: RealtimePublisher = state.realtime_publisher
    scheduler.enqueue(lambda: _run_demo_job(job_id, publisher))


async def _run_demo_job(job_id: UUID, publisher: RealtimePublisher) -> None:
    for status, progress in _DEMO_JOB_FRAMES:
        await publisher.publish_to_job(
            job_id,
            JobProgressMessage(job_id=job_id, status=status, progress=progress, occurred_at=datetime.now(timezone.utc)),
        )
        await asyncio.sleep(0.25)
