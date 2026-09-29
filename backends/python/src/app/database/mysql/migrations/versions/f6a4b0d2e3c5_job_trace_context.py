"""job trace context

Revision ID: f6a4b0d2e3c5
Revises: 762f23e14ca7
Create Date: 2026-09-30 00:00:00.000000

"""
from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa


revision: str = 'f6a4b0d2e3c5'
down_revision: Union[str, None] = '762f23e14ca7'
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
