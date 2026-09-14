"""The httpOnly refresh-token cookie every auth endpoint that issues or
consumes a token pair reads or writes — see ``contract/openapi.yaml``:
``refreshToken=<value>; HttpOnly; Secure; SameSite=Strict; Path=/v1/auth``.
``Secure`` is set unconditionally to match the contract text exactly; a
browser only honours it over HTTPS, which is how this is actually served
outside local development.
"""

from __future__ import annotations

from fastapi import Request, Response

COOKIE_NAME = "refreshToken"
_COOKIE_PATH = "/v1/auth"


def set_refresh_cookie(response: Response, raw_refresh_token: str) -> None:
    response.set_cookie(
        key=COOKIE_NAME,
        value=raw_refresh_token,
        httponly=True,
        secure=True,
        samesite="strict",
        path=_COOKIE_PATH,
    )


def clear_refresh_cookie(response: Response) -> None:
    response.delete_cookie(key=COOKIE_NAME, path=_COOKIE_PATH)


def read_refresh_cookie(request: Request) -> str | None:
    return request.cookies.get(COOKIE_NAME)
