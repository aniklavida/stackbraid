"""Pydantic request models — the inbound half of the contract. Field-level
constraints mirror ``contract/openapi.yaml`` exactly, so a violation is a
FastAPI/Pydantic ``RequestValidationError`` translated by
``shared/web/exception_handling.py`` into the Problem envelope.

Email format uses a plain pattern, not Pydantic's ``EmailStr`` — that type
pulls in ``email-validator``'s deliverability/special-use-domain checks
(DNS lookups, rejecting ``.local``/``.test``/reserved TLDs), which is both an
unwanted network dependency at validation time and, concretely, would lock
the ``@stackbraid.local`` seed accounts out of their own login endpoint.
The pattern below is the same shape the contract's ``format: email`` and
this feature's domain ``Email`` value object both already enforce.
"""

from __future__ import annotations

from uuid import UUID

from pydantic import Field, model_validator

from app.features.identity.contracts.camel import CamelModel

_EMAIL_PATTERN = r"^[^@\s]+@[^@\s]+\.[^@\s]+$"


class RegisterRequest(CamelModel):
    email: str = Field(pattern=_EMAIL_PATTERN)
    password: str = Field(min_length=8)
    display_name: str = Field(min_length=1, max_length=200)


class LoginRequest(CamelModel):
    email: str
    password: str


class RefreshRequest(CamelModel):
    refresh_token: str | None = None


class UpdateUserRequest(CamelModel):
    display_name: str | None = Field(default=None, min_length=1, max_length=200)
    email: str | None = Field(default=None, pattern=_EMAIL_PATTERN)

    @model_validator(mode="after")
    def at_least_one_field(self) -> "UpdateUserRequest":
        if self.display_name is None and self.email is None:
            raise ValueError("at least one field must be provided")
        return self


class AssignRoleRequest(CamelModel):
    role_id: UUID
