"""Seeds exactly what a fresh SQL Server database needs to be usable: two
roles and two accounts. Idempotent — checked by presence, so running it
against an already-seeded database (every ordinary startup, after the first)
is a no-op rather than a duplicate-key error.
"""

from __future__ import annotations

from datetime import datetime, timezone

from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from app.features.identity.domain.entities import Role, User
from app.features.identity.domain.value_objects import Email
from app.features.identity.persistence.repositories import SqlAlchemyRoleRepository, SqlAlchemyUserRepository
from app.shared.security.password_hasher import PasswordHasher

ADMIN_EMAIL = "admin@stackbraid.local"
STANDARD_USER_EMAIL = "user@stackbraid.local"

# Only used for the seeded accounts in a fresh, non-production database —
# see backends/python/README.md. Never used for an account a real user
# registers.
SEED_PASSWORD = "ChangeMe!123"  # noqa: S105 - a documented, non-secret development default


async def seed(session_factory: async_sessionmaker[AsyncSession], password_hasher: PasswordHasher) -> None:
    async with session_factory() as session:
        users = SqlAlchemyUserRepository(session)
        roles = SqlAlchemyRoleRepository(session)

        if await users.exists_by_email(Email.create(ADMIN_EMAIL)):
            return  # Already seeded.

        now = datetime.now(timezone.utc)

        admin_role = Role.create("admin", "Full administrative access.", ["users:read", "users:write", "roles:read", "roles:write"])
        admin_role.created_at = admin_role.updated_at = now
        user_role = Role.create("user", "An ordinary authenticated account.", ["users:read:self"])
        user_role.created_at = user_role.updated_at = now

        await roles.add(admin_role)
        await roles.add(user_role)

        admin = User.register(Email.create(ADMIN_EMAIL), password_hasher.hash(SEED_PASSWORD), "Administrator", now)
        admin.created_at = admin.updated_at = now
        admin.assign_role(admin_role, now)

        standard_user = User.register(Email.create(STANDARD_USER_EMAIL), password_hasher.hash(SEED_PASSWORD), "Standard User", now)
        standard_user.created_at = standard_user.updated_at = now
        standard_user.assign_role(user_role, now)

        await users.add(admin)
        await users.add(standard_user)

        await session.commit()
