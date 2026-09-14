"""RFC 3339, UTC, always suffixed ``Z`` — never a numeric offset such as
``+00:00``. A spike caught .NET emitting the offset form while Python's own
``datetime.isoformat()`` emits it too; ``contract/openapi.yaml``'s
``UtcDateTime`` schema pins the ``Z`` form with an explicit pattern, so this
is the one place that formatting is produced instead of left to chance.
"""

from __future__ import annotations

from datetime import datetime, timezone
from typing import Annotated

from pydantic import PlainSerializer


def to_utc_z(value: datetime) -> str:
    aware = value if value.tzinfo is not None else value.replace(tzinfo=timezone.utc)
    aware = aware.astimezone(timezone.utc)
    base = aware.strftime("%Y-%m-%dT%H:%M:%S")
    if aware.microsecond:
        base += f".{aware.microsecond:06d}"
    return base + "Z"


UtcDateTime = Annotated[datetime, PlainSerializer(to_utc_z, return_type=str)]
