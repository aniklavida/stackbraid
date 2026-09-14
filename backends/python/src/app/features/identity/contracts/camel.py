"""Every DTO and request in this feature is camelCase on the wire (the
contract is one file shared with .NET) and snake_case in Python source — this
base model is the one place that translates between the two conventions.
"""

from __future__ import annotations

from pydantic import BaseModel, ConfigDict


def to_camel(name: str) -> str:
    first, *rest = name.split("_")
    return first + "".join(word.capitalize() for word in rest)


class CamelModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)
