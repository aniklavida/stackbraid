"""shared jobs table

Revision ID: c3a1f7d9e2b4
Revises: 79c02430f38c
Create Date: 2026-09-24 09:00:00.000000

"""
from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa


# revision identifiers, used by Alembic.
revision: str = 'c3a1f7d9e2b4'
down_revision: Union[str, None] = '79c02430f38c'
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    op.create_table(
        'shared_jobs',
        sa.Column('id', sa.Uuid(), nullable=False),
        sa.Column('type', sa.String(length=200), nullable=False),
        sa.Column('payload', sa.Text(), nullable=False),
        sa.Column('owner_id', sa.String(length=200), nullable=True),
        sa.Column('state', sa.String(length=20), nullable=False),
        sa.Column('attempts', sa.Integer(), nullable=False, server_default='0'),
        sa.Column('max_attempts', sa.Integer(), nullable=False, server_default='3'),
        sa.Column('last_error', sa.Text(), nullable=True),
        sa.Column('created_at', sa.DateTime(timezone=True), nullable=False),
        sa.Column('updated_at', sa.DateTime(timezone=True), nullable=False),
        sa.Column('next_attempt_at', sa.DateTime(timezone=True), nullable=True),
        sa.PrimaryKeyConstraint('id'),
    )
    op.create_index('ix_shared_jobs_owner_id', 'shared_jobs', ['owner_id'], unique=False)
    op.create_index('ix_shared_jobs_state_next_attempt_at', 'shared_jobs', ['state', 'next_attempt_at'], unique=False)


def downgrade() -> None:
    op.drop_index('ix_shared_jobs_state_next_attempt_at', table_name='shared_jobs')
    op.drop_index('ix_shared_jobs_owner_id', table_name='shared_jobs')
    op.drop_table('shared_jobs')
