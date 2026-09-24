"""SQLAlchemy ORM mapping for the Identity feature. Provider-agnostic on
purpose — every column type below (``Uuid``, ``JSON``, ``Enum``, mapped
Python ``datetime``) is portable across Postgres, SQL Server and MySQL; the
only place a provider name may appear is ``app/database/<provider>/``, per
``STRUCTURE.md``. These are persistence rows, not the domain: ``domain/``
never imports this module, and a repository is the only thing that
translates between the two.
"""

from __future__ import annotations

import json
import uuid
from datetime import datetime, timezone

from sqlalchemy import JSON, DateTime, ForeignKey, String, Text, Uuid, event
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.shared.persistence.base import OrmBase


class UserModel(OrmBase):
    __tablename__ = "identity_users"

    id: Mapped[uuid.UUID] = mapped_column(Uuid, primary_key=True)
    email: Mapped[str] = mapped_column(String(320), unique=True, index=True, nullable=False)
    password_hash: Mapped[str] = mapped_column(String(256), nullable=False)
    display_name: Mapped[str] = mapped_column(String(200), nullable=False)
    # Stored as its plain string value ("active"/"inactive") rather than a
    # mapped Python enum — the repository is the only place that translates
    # to and from ``UserStatus``, keeping this row's column types generic.
    status: Mapped[str] = mapped_column(String(20), nullable=False)
    last_login_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    updated_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    deleted_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True, index=True)

    user_roles: Mapped[list["UserRoleModel"]] = relationship(
        back_populates="user", cascade="all, delete-orphan", lazy="selectin"
    )


class RoleModel(OrmBase):
    __tablename__ = "identity_roles"

    id: Mapped[uuid.UUID] = mapped_column(Uuid, primary_key=True)
    name: Mapped[str] = mapped_column(String(100), unique=True, nullable=False)
    description: Mapped[str | None] = mapped_column(String(500), nullable=True)
    # Stored as a JSON array of permission-code strings — a generic type
    # every shipped provider supports, so this row never needs a
    # provider-specific column type.
    permissions: Mapped[list[str]] = mapped_column(JSON, nullable=False)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    updated_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)


class UserRoleModel(OrmBase):
    __tablename__ = "identity_user_roles"

    user_id: Mapped[uuid.UUID] = mapped_column(Uuid, ForeignKey("identity_users.id"), primary_key=True)
    role_id: Mapped[uuid.UUID] = mapped_column(Uuid, ForeignKey("identity_roles.id"), primary_key=True)
    assigned_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)

    user: Mapped[UserModel] = relationship(back_populates="user_roles")
    role: Mapped[RoleModel] = relationship(lazy="selectin")


class RefreshTokenModel(OrmBase):
    __tablename__ = "identity_refresh_tokens"

    id: Mapped[uuid.UUID] = mapped_column(Uuid, primary_key=True)
    user_id: Mapped[uuid.UUID] = mapped_column(Uuid, ForeignKey("identity_users.id"), nullable=False, index=True)
    token_hash: Mapped[str] = mapped_column(String(64), unique=True, nullable=False, index=True)
    expires_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    revoked_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
    replaced_by_token_id: Mapped[uuid.UUID | None] = mapped_column(Uuid, nullable=True)

    # A relationship, not just the column-level ForeignKey above, on
    # purpose: SQLAlchemy's unit of work only orders INSERT statements
    # across two mapped classes by their foreign-key dependency when an ORM
    # relationship links them — the raw column constraint alone is not
    # enough. Without this, a token added in the same flush as a brand-new
    # user can be inserted first and fail its own foreign key check.
    user: Mapped[UserModel] = relationship()


class AuditLogModel(OrmBase):
    __tablename__ = "identity_audit_logs"

    id: Mapped[uuid.UUID] = mapped_column(Uuid, primary_key=True)
    entity_type: Mapped[str] = mapped_column(String(100), nullable=False)
    entity_id: Mapped[uuid.UUID] = mapped_column(Uuid, nullable=False, index=True)
    action: Mapped[str] = mapped_column(String(50), nullable=False)
    actor_id: Mapped[uuid.UUID | None] = mapped_column(Uuid, nullable=True)
    correlation_id: Mapped[str] = mapped_column(String(200), nullable=False)
    occurred_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    details: Mapped[str | None] = mapped_column(Text, nullable=True)


@event.listens_for(type(UserModel.__mapper__), "after_configured")
def _configure_audit_and_soft_delete_listeners() -> None:
    from sqlalchemy.orm import Session
    from sqlalchemy.orm.attributes import get_history

    from app.shared.auditing.audit import AuditEntry
    from app.shared.observability.context import actor_id_var, correlation_id_var

    @event.listens_for(Session, "do_orm_execute")
    def _exclude_deleted_users(execute_state) -> None:
        if execute_state.is_select and execute_state.execution_options.get("include_deleted") is not True:
            statement = execute_state.statement
            if "identity_users" in str(statement):
                execute_state.statement = statement.where(UserModel.deleted_at.is_(None))

    @event.listens_for(Session, "before_flush")
    def _record_user_changes(session, _flush_context, _instances) -> None:
        now = datetime.now(timezone.utc)
        correlation_id = correlation_id_var.get() or "unknown"
        actor_id = actor_id_var.get()
        for row in set(session.new).union(session.dirty).union(session.deleted):
            if not isinstance(row, UserModel):
                continue
            if row in session.new:
                action = "created"
            elif row in session.deleted:
                action = "deleted"
            else:
                deleted_history = get_history(row, "deleted_at")
                action = "deleted" if deleted_history.added and deleted_history.added[0] is not None else "restored" if deleted_history.deleted else "updated"
            session.add(AuditLogModel(
                id=uuid.uuid4(),
                entity_type="User",
                entity_id=row.id,
                action=action,
                actor_id=actor_id,
                correlation_id=correlation_id,
                occurred_at=now,
                details=json.dumps({"status": row.status}, sort_keys=True),
            ))
