"""The failure shape every handler returns instead of raising, for an
expected, nameable condition ("email already registered", "user not found").

``code`` is the stable string the contract's ``Problem.code`` field carries —
see ``contract/openapi.yaml``. ``message_key`` is a localization resource key;
a handler never renders human text itself, so the same error reads correctly
in every configured locale.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from enum import Enum
from typing import Mapping


class AppErrorType(str, Enum):
    VALIDATION = "validation"
    NOT_FOUND = "not_found"
    CONFLICT = "conflict"
    UNAUTHORIZED = "unauthorized"
    FORBIDDEN = "forbidden"
    FAILURE = "failure"


@dataclass(frozen=True, slots=True)
class AppError:
    code: str
    type: AppErrorType
    message_key: str
    arguments: Mapping[str, str] | None = None
    field_errors: Mapping[str, list[str]] | None = field(default=None)

    @staticmethod
    def not_found(code: str, message_key: str) -> "AppError":
        return AppError(code, AppErrorType.NOT_FOUND, message_key)

    @staticmethod
    def conflict(code: str, message_key: str) -> "AppError":
        return AppError(code, AppErrorType.CONFLICT, message_key)

    @staticmethod
    def unauthorized(code: str, message_key: str) -> "AppError":
        return AppError(code, AppErrorType.UNAUTHORIZED, message_key)

    @staticmethod
    def forbidden(code: str, message_key: str) -> "AppError":
        return AppError(code, AppErrorType.FORBIDDEN, message_key)

    @staticmethod
    def validation(code: str, message_key: str, field_errors: Mapping[str, list[str]]) -> "AppError":
        return AppError(code, AppErrorType.VALIDATION, message_key, field_errors=field_errors)

    @staticmethod
    def failure(code: str, message_key: str) -> "AppError":
        return AppError(code, AppErrorType.FAILURE, message_key)
