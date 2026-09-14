"""The last line of defence: any exception a handler did not turn into an
``AppError`` lands here and still comes back as a contract-shaped Problem,
never a bare traceback. Expected failures (validation, not-found, ...) should
never reach this — they are ``Result`` failures mapped by the endpoint itself.
"""

from __future__ import annotations

import logging

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from starlette.responses import JSONResponse

from app.shared.localization.localizer import AppLocalizer
from app.shared.web.errors import AppError, AppErrorType
from app.shared.web.problem import problem_response

logger = logging.getLogger("stackbraid.unhandled")


def register_exception_handlers(app: FastAPI, localizer: AppLocalizer) -> None:
    @app.exception_handler(RequestValidationError)
    async def handle_validation_error(request: Request, exc: RequestValidationError) -> JSONResponse:
        field_errors: dict[str, list[str]] = {}
        for item in exc.errors():
            field = ".".join(str(part) for part in item["loc"] if part not in ("body", "query", "path"))
            field_errors.setdefault(field or "request", []).append(item["msg"])

        error = AppError(
            code="IDENTITY.VALIDATION_FAILED",
            type=AppErrorType.VALIDATION,
            message_key="identity.validation_failed",
            field_errors={key: [_literal(msg) for msg in msgs] for key, msgs in field_errors.items()},
        )
        return problem_response(error, localizer, request)

    @app.exception_handler(Exception)
    async def handle_unhandled_exception(request: Request, exc: Exception) -> JSONResponse:
        logger.exception("Unhandled exception while processing %s %s", request.method, request.url.path)
        error = AppError.failure("SYSTEM.UNEXPECTED_ERROR", "problem.server_error.detail")
        return problem_response(error, localizer, request)


def _literal(message: str) -> str:
    """FastAPI/Pydantic validation messages are already plain English, not a
    localization key — the localizer resolves an unknown key to itself, which
    is exactly this pass-through behaviour.
    """

    return message
