"""SQLAlchemy ORM mapping for the Identity feature. Provider-agnostic on
purpose — every column type below (``Uuid``, ``JSON``, ``Enum``, mapped
Python ``datetime``) is portable across Postgres, SQL Server and MySQL; the
only place a provider name may appear is ``app/database/<provider>/``, per
``STRUCTURE.md``. These are persistence rows, not the domain: ``domain/``
never imports this module, and a repository is the only thing that
translates between the two.
"""

from __future__ import annotations

import uuid
from datetime import datetime

from sqlalchemy import JSON, DateTime, ForeignKey, String, Uuid
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
