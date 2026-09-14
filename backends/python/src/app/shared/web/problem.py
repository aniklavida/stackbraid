"""Turns an ``AppError`` into the RFC 9457 envelope ``contract/openapi.yaml``'s
``Problem`` schema pins: the required ``type``/``title``/``status`` core, plus
this contract's ``code``/``traceId``/``errors`` extension. ``title`` and
``detail`` are localized; ``code`` never is — a client branches on ``code``,
never on the sentence.
"""

from __future__ import annotations

from starlette.requests import Request
from starlette.responses import JSONResponse

from app.shared.localization.localizer import AppLocalizer
from app.shared.web.correlation import get_correlation_id
from app.shared.web.errors import AppError, AppErrorType

_STATUS_CODES: dict[AppErrorType, int] = {
    AppErrorType.VALIDATION: 400,
    AppErrorType.UNAUTHORIZED: 401,
    AppErrorType.FORBIDDEN: 403,
    AppErrorType.NOT_FOUND: 404,
    AppErrorType.CONFLICT: 409,
    AppErrorType.FAILURE: 500,
}

_TITLE_KEYS: dict[AppErrorType, str] = {
    AppErrorType.VALIDATION: "problem.validation_failed.title",
    AppErrorType.UNAUTHORIZED: "problem.unauthorized.title",
    AppErrorType.FORBIDDEN: "problem.forbidden.title",
    AppErrorType.NOT_FOUND: "problem.not_found.title",
    AppErrorType.CONFLICT: "problem.conflict.title",
    AppErrorType.FAILURE: "problem.server_error.title",
}


def status_code_for(error_type: AppErrorType) -> int:
    return _STATUS_CODES.get(error_type, 500)


def to_problem_body(
    error: AppError,
    localizer: AppLocalizer,
    culture: str | None,
    trace_id: str,
    instance: str,
) -> dict:
    status = status_code_for(error.type)
    body: dict = {
        "type": "about:blank",
        "title": localizer.get_string(_TITLE_KEYS.get(error.type, "problem.server_error.title"), culture),
        "status": status,
        "detail": localizer.get_string(error.message_key, culture, error.arguments),
        "instance": instance,
        "code": error.code,
        "traceId": trace_id,
    }

    if error.type == AppErrorType.VALIDATION and error.field_errors:
        body["errors"] = {
            field: [localizer.get_string(msg_key, culture) for msg_key in messages]
            for field, messages in error.field_errors.items()
        }

    return body


def problem_response(
    error: AppError,
    localizer: AppLocalizer,
    request: Request,
    status_override: int | None = None,
) -> JSONResponse:
    """Builds the actual HTTP response for an ``AppError`` raised or returned
    inside a request. Reads the correlation ID and ``Accept-Language`` off the
    request so a handler never has to thread them through by hand.
    """

    culture = request.headers.get("accept-language")
    trace_id = get_correlation_id(request)
    body = to_problem_body(error, localizer, culture, trace_id, str(request.url.path))
    status = status_override or body["status"]
    body["status"] = status
    return JSONResponse(status_code=status, content=body, media_type="application/problem+json")
