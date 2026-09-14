"""A copy, not an import, of ``app.features.identity.contracts.camel`` and
``datetime_utils`` — ``Shared`` never imports a feature (see
``pyproject.toml``'s import-linter contracts), and a realtime message is
exactly the kind of thing a future second feature will also want to push,
so this small amount of camelCase/UTC-``Z`` plumbing belongs here rather
than borrowed from ``Identity``.
"""

from __future__ import annotations

from datetime import datetime, timezone
from typing import Annotated

from pydantic import BaseModel, ConfigDict, PlainSerializer


def to_camel(name: str) -> str:
    first, *rest = name.split("_")
    return first + "".join(word.capitalize() for word in rest)


class CamelModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


def to_utc_z(value: datetime) -> str:
    aware = value if value.tzinfo is not None else value.replace(tzinfo=timezone.utc)
    aware = aware.astimezone(timezone.utc)
    base = aware.strftime("%Y-%m-%dT%H:%M:%S")
    if aware.microsecond:
        base += f".{aware.microsecond:06d}"
    return base + "Z"


UtcDateTime = Annotated[datetime, PlainSerializer(to_utc_z, return_type=str)]
