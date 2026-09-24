"""Implements the Identity domain's repository protocols against SQLAlchemy,
translating between the ORM rows in ``models.py`` and the plain domain
entities in ``domain/entities.py``. The domain never sees a SQLAlchemy
object; this module is the only place that knows both shapes exist.
"""

from __future__ import annotations

from typing import Sequence
from uuid import UUID

from sqlalchemy import func, or_, select
from sqlalchemy.ext.asyncio import AsyncSession

from app.features.identity.domain.entities import RefreshToken, Role, User, UserRole, UserStatus
from app.features.identity.domain.repositories import UserSearchQuery, UserSearchResult
from app.features.identity.domain.value_objects import Email
from app.features.identity.persistence.models import AuditLogModel, RefreshTokenModel, RoleModel, UserModel, UserRoleModel
from app.shared.auditing.audit import AuditEntry

_SORTABLE_COLUMNS = {
    "email": UserModel.email,
    "displayName": UserModel.display_name,
    "createdAt": UserModel.created_at,
    "status": UserModel.status,
}


def _role_to_domain(row: RoleModel) -> Role:
    role = Role(row.id, row.name, row.description, row.permissions)
    role.created_at = row.created_at
    role.updated_at = row.updated_at
    return role


def _user_to_domain(row: UserModel) -> User:
    user = User(row.id, Email(row.email), row.password_hash, row.display_name)
    user.status = UserStatus(row.status)
    user.last_login_at = row.last_login_at
    user.deleted_at = row.deleted_at
    user.created_at = row.created_at
    user.updated_at = row.updated_at
    for link in row.user_roles:
        role = _role_to_domain(link.role)
        user._user_roles.append(UserRole(user.id, role, link.assigned_at))  # noqa: SLF001 - repository is the mapping boundary
    return user


class SqlAlchemyUserRepository:
    def __init__(self, session: AsyncSession) -> None:
        self._session = session

    async def get_by_id(self, id: UUID, include_deleted: bool = False) -> User | None:
        statement = select(UserModel).where(UserModel.id == id)
        if not include_deleted:
            statement = statement.where(UserModel.deleted_at.is_(None))
        result = await self._session.execute(statement.execution_options(include_deleted=include_deleted))
        row = result.scalar_one_or_none()
        return _user_to_domain(row) if row else None

    async def get_by_email(self, email: Email) -> User | None:
        result = await self._session.execute(
            select(UserModel).where(UserModel.email == email.value, UserModel.deleted_at.is_(None))
        )
        row = result.scalar_one_or_none()
        return _user_to_domain(row) if row else None

    async def exists_by_email(self, email: Email) -> bool:
        result = await self._session.execute(select(func.count()).select_from(UserModel).where(UserModel.email == email.value))
        return (result.scalar_one() or 0) > 0

    async def add(self, user: User) -> None:
        row = UserModel(
            id=user.id,
            email=user.email.value,
            password_hash=user.password_hash,
            display_name=user.display_name,
            status=user.status.value,
            last_login_at=user.last_login_at,
            created_at=user.created_at,
            updated_at=user.updated_at,
            deleted_at=user.deleted_at,
        )
        self._session.add(row)
        await self._sync_user_roles(user, row)

    async def _sync_user_roles(self, user: User, row: UserModel) -> None:
        """Reconciles the join table against the domain entity's in-memory
        role links — used both when a brand-new user is added and when an
        existing user's roles changed during this unit of work.
        """

        existing_role_ids = {link.role_id for link in row.user_roles}
        wanted_role_ids = {link.role_id for link in user.user_roles}

        for link in list(row.user_roles):
            if link.role_id not in wanted_role_ids:
                row.user_roles.remove(link)

        for link in user.user_roles:
            if link.role_id not in existing_role_ids:
                row.user_roles.append(UserRoleModel(user_id=user.id, role_id=link.role_id, assigned_at=link.assigned_at))

    async def save_changes(self, user: User) -> None:
        """Flushes profile/status/role mutations on an already-persisted
        user back onto its row. A command handler calls this after mutating
        a domain entity it loaded through ``get_by_id``/``get_by_email``.
        """

        row = await self._session.get(UserModel, user.id)
        if row is None:
            raise ValueError(f"User {user.id} was not found for an update.")

        row.email = user.email.value
        row.display_name = user.display_name
        row.status = user.status.value
        row.last_login_at = user.last_login_at
        row.updated_at = user.updated_at
        row.deleted_at = user.deleted_at
        await self._sync_user_roles(user, row)

    async def search(self, query: UserSearchQuery) -> UserSearchResult:
        stmt = select(UserModel)
        count_stmt = select(func.count()).select_from(UserModel)
        if not query.include_deleted:
            stmt = stmt.where(UserModel.deleted_at.is_(None))
            count_stmt = count_stmt.where(UserModel.deleted_at.is_(None))

        if query.status is not None:
            stmt = stmt.where(UserModel.status == query.status.value)
            count_stmt = count_stmt.where(UserModel.status == query.status.value)

        if query.search:
            pattern = f"%{query.search.lower()}%"
            condition = or_(func.lower(UserModel.email).like(pattern), func.lower(UserModel.display_name).like(pattern))
            stmt = stmt.where(condition)
            count_stmt = count_stmt.where(condition)

        if query.role_id is not None:
            stmt = stmt.join(UserModel.user_roles).where(UserRoleModel.role_id == query.role_id)
            count_stmt = count_stmt.join(UserModel.user_roles).where(UserRoleModel.role_id == query.role_id)

        descending = query.sort.startswith("-")
        field_name = query.sort[1:] if descending else query.sort
        column = _SORTABLE_COLUMNS.get(field_name, UserModel.created_at)
        stmt = stmt.order_by(column.desc() if descending else column.asc())

        stmt = stmt.offset((query.page - 1) * query.page_size).limit(query.page_size)

        total = (await self._session.execute(count_stmt.execution_options(include_deleted=query.include_deleted))).scalar_one()
        rows = (await self._session.execute(stmt.execution_options(include_deleted=query.include_deleted))).scalars().unique().all()

        return UserSearchResult(items=[_user_to_domain(row) for row in rows], total_count=total)


class SqlAlchemyAuditLog:
    def __init__(self, session: AsyncSession) -> None:
        self._session = session

    def add(self, entry: AuditEntry) -> None:
        self._session.add(AuditLogModel(
            id=entry.id,
            entity_type=entry.entity_type,
            entity_id=entry.entity_id,
            action=entry.action,
            actor_id=entry.actor_id,
            correlation_id=entry.correlation_id,
            occurred_at=entry.occurred_at,
            details=entry.details,
        ))

    async def list(self, entity_id: UUID) -> list[AuditEntry]:
        result = await self._session.execute(
            select(AuditLogModel)
            .where(AuditLogModel.entity_id == entity_id)
            .order_by(AuditLogModel.occurred_at.desc())
        )
        return [
            AuditEntry(
                id=row.id,
                entity_type=row.entity_type,
                entity_id=row.entity_id,
                action=row.action,
                actor_id=row.actor_id,
                correlation_id=row.correlation_id,
                occurred_at=row.occurred_at,
                details=row.details,
            )
            for row in result.scalars().all()
        ]


class SqlAlchemyRoleRepository:
    def __init__(self, session: AsyncSession) -> None:
        self._session = session

    async def get_by_id(self, id: UUID) -> Role | None:
        row = await self._session.get(RoleModel, id)
        return _role_to_domain(row) if row else None

    async def list_all(self) -> Sequence[Role]:
        result = await self._session.execute(select(RoleModel).order_by(RoleModel.name))
        return [_role_to_domain(row) for row in result.scalars().all()]

    async def add(self, role: Role) -> None:
        self._session.add(
            RoleModel(
                id=role.id,
                name=role.name,
                description=role.description,
                permissions=list(role.permissions),
                created_at=role.created_at,
                updated_at=role.updated_at,
            )
        )


class SqlAlchemyRefreshTokenRepository:
    def __init__(self, session: AsyncSession) -> None:
        self._session = session

    async def get_by_token_hash(self, token_hash: str) -> RefreshToken | None:
        result = await self._session.execute(select(RefreshTokenModel).where(RefreshTokenModel.token_hash == token_hash))
        row = result.scalar_one_or_none()
        if row is None:
            return None
        token = RefreshToken(row.id, row.user_id, row.token_hash, row.expires_at, row.created_at)
        token.revoked_at = row.revoked_at
        token.replaced_by_token_id = row.replaced_by_token_id
        return token

    async def add(self, token: RefreshToken) -> None:
        self._session.add(
            RefreshTokenModel(
                id=token.id,
                user_id=token.user_id,
                token_hash=token.token_hash,
                expires_at=token.expires_at,
                created_at=token.created_at,
                revoked_at=token.revoked_at,
                replaced_by_token_id=token.replaced_by_token_id,
            )
        )

    async def save_changes(self, token: RefreshToken) -> None:
        row = await self._session.get(RefreshTokenModel, token.id)
        if row is None:
            raise ValueError(f"Refresh token {token.id} was not found for an update.")
        row.revoked_at = token.revoked_at
        row.replaced_by_token_id = token.replaced_by_token_id
