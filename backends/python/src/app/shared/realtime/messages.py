"""Mirrors ``contract/openapi.yaml``'s ``RealtimeMessage`` discriminated
union exactly — the JSON on the wire must be byte-identical to what the
.NET backend's own ``Shared/Realtime/RealtimeMessages.cs`` produces, whatever
each backend's own transport (native WebSockets here, SignalR there).
"""

from __future__ import annotations

from typing import Literal
from uuid import UUID

from app.shared.realtime.camel import CamelModel, UtcDateTime


class RoleSummary(CamelModel):
    id: UUID
    name: str
    description: str | None = None
    permissions: list[str]


class UserDeactivatedMessage(CamelModel):
    type: Literal["user.deactivated"] = "user.deactivated"
    user_id: UUID
    occurred_at: UtcDateTime


class UserRoleChangedMessage(CamelModel):
    type: Literal["user.role_changed"] = "user.role_changed"
    user_id: UUID
    roles: list[RoleSummary]
    occurred_at: UtcDateTime


class JobProgressMessage(CamelModel):
    type: Literal["job.progress"] = "job.progress"
    job_id: UUID
    status: Literal["queued", "running", "succeeded", "failed"]
    progress: int
    occurred_at: UtcDateTime


RealtimeMessage = UserDeactivatedMessage | UserRoleChangedMessage | JobProgressMessage
