"""job trace context

Revision ID: a7b5c1e3f4d6
Revises: 082f0e71072b
Create Date: 2026-09-30 00:00:00.000000

"""
from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa


revision: str = 'a7b5c1e3f4d6'
down_revision: Union[str, None] = '082f0e71072b'
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    op.add_column('shared_jobs', sa.Column('correlation_id', sa.String(length=64), nullable=True))
    op.add_column('shared_jobs', sa.Column('trace_parent', sa.String(length=128), nullable=True))
    op.add_column('shared_jobs', sa.Column('trace_state', sa.String(length=512), nullable=True))


def downgrade() -> None:
    op.drop_column('shared_jobs', 'trace_state')
    op.drop_column('shared_jobs', 'trace_parent')
    op.drop_column('shared_jobs', 'correlation_id')
