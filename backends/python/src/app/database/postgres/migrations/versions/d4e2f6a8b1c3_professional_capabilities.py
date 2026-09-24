"""professional capabilities

Revision ID: d4e2f6a8b1c3
Revises: c3a1f7d9e2b4
Create Date: 2026-09-25 00:00:00.000000

"""
from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa


revision: str = 'd4e2f6a8b1c3'
down_revision: Union[str, None] = 'c3a1f7d9e2b4'
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    op.add_column('identity_users', sa.Column('deleted_at', sa.DateTime(timezone=True), nullable=True))
    op.create_index(op.f('ix_identity_users_deleted_at'), 'identity_users', ['deleted_at'], unique=False)
    op.create_table(
        'identity_audit_logs',
        sa.Column('id', sa.Uuid(), nullable=False),
        sa.Column('entity_type', sa.String(length=100), nullable=False),
        sa.Column('entity_id', sa.Uuid(), nullable=False),
        sa.Column('action', sa.String(length=50), nullable=False),
        sa.Column('actor_id', sa.Uuid(), nullable=True),
        sa.Column('correlation_id', sa.String(length=200), nullable=False),
        sa.Column('occurred_at', sa.DateTime(timezone=True), nullable=False),
        sa.Column('details', sa.String(), nullable=True),
        sa.PrimaryKeyConstraint('id'),
    )
    op.create_index(op.f('ix_identity_audit_logs_entity_id'), 'identity_audit_logs', ['entity_id'], unique=False)


def downgrade() -> None:
    op.drop_index(op.f('ix_identity_audit_logs_entity_id'), table_name='identity_audit_logs')
    op.drop_table('identity_audit_logs')
    op.drop_index(op.f('ix_identity_users_deleted_at'), table_name='identity_users')
    op.drop_column('identity_users', 'deleted_at')
