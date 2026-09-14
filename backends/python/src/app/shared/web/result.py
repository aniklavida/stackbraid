"""A handler's outcome: exactly one of a value or an ``AppError``.

Endpoints translate a failure into the contract's Problem envelope (see
``problem.py``) and a success into the response schema directly — the
contract has no generic success wrapper, only DTOs.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Callable, Generic, TypeVar

from app.shared.web.errors import AppError

T = TypeVar("T")
R = TypeVar("R")


@dataclass(frozen=True, slots=True)
class Result(Generic[T]):
    _value: T | None
    _error: AppError | None
    is_success: bool

    @staticmethod
    def success(value: T) -> "Result[T]":
        return Result(value, None, True)

    @staticmethod
    def failure(error: AppError) -> "Result[T]":
        return Result(None, error, False)

    @property
    def value(self) -> T:
        if not self.is_success:
            raise ValueError("Result has no value — it is a failure.")
        return self._value  # type: ignore[return-value]

    @property
    def error(self) -> AppError:
        if self.is_success:
            raise ValueError("Result has no error — it is a success.")
        return self._error  # type: ignore[return-value]

    def match(self, on_success: Callable[[T], R], on_failure: Callable[[AppError], R]) -> R:
        return on_success(self._value) if self.is_success else on_failure(self._error)  # type: ignore[arg-type]
