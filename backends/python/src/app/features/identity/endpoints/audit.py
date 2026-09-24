from __future__ import annotations

from uuid import UUID

from fastapi import APIRouter, Depends, Query

from app.features.identity.contracts.dtos import AuditEntryDto, AuditPageDto
from app.features.identity.endpoints.dependencies import get_audit_log
from app.features.identity.endpoints.security import require_permission
from app.features.identity.persistence.repositories import SqlAlchemyAuditLog

router = APIRouter(prefix="/v1/audit", tags=["Audit"])


@router.get("", dependencies=[Depends(require_permission("users:read"))])
async def list_audit(user_id: UUID = Query(alias="userId"), audit_log: SqlAlchemyAuditLog = Depends(get_audit_log)) -> AuditPageDto:
    entries = await audit_log.list(user_id)
    return AuditPageDto(items=[AuditEntryDto(
        id=entry.id,
        entity_type=entry.entity_type,
        entity_id=entry.entity_id,
        action=entry.action,
        actor_id=entry.actor_id,
        correlation_id=entry.correlation_id,
        occurred_at=entry.occurred_at,
        details=entry.details,
    ) for entry in entries])
