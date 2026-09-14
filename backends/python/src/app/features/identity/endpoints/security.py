"""The real implementation of the ``AccessTokenIssuer`` port ``application``
declares — everything JWT-specific (signing, claims, expiry) lives here, at
the edge, exactly as ``contract/openapi.yaml``'s ``bearerAuth`` security
scheme describes. Also the FastAPI dependencies that verify a token on the
way back in and enforce a required permission.
"""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from uuid import UUID

import jwt
from fastapi import Depends, HTTPException, Request

from app.features.identity.application.abstractions import IssuedAccessToken
from app.shared.web.errors import AppError

PERMISSION_CLAIM = "permission"


@dataclass(frozen=True, slots=True)
class JwtOptions:
    # Base64-encoded HMAC-SHA256 signing key. The development default in
    # host/config.py is fixed and publicly known — never used past local
    # development, replaced by a real secret (environment variable or
    # secret store) in any deployed environment.
    signing_key: str
    issuer: str = "stackbraid"
    audience: str = "stackbraid-clients"
    # In seconds, not minutes — the conformance suite's expired-token check
    # needs a genuinely short-lived token to exercise for real.
    access_token_lifetime_seconds: int = 900


class JwtAccessTokenIssuer:
    def __init__(self, options: JwtOptions) -> None:
        self._options = options

    def issue(self, user_id: UUID, roles: list[str], permissions: list[str]) -> IssuedAccessToken:
        now = datetime.now(timezone.utc)
        expires_at = now + timedelta(seconds=self._options.access_token_lifetime_seconds)

        payload = {
            "sub": str(user_id),
            "iss": self._options.issuer,
            "aud": self._options.audience,
            "nbf": now,
            "iat": now,
            "exp": expires_at,
            PERMISSION_CLAIM: permissions,
        }

        token = jwt.encode(payload, self._options.signing_key, algorithm="HS256")
        return IssuedAccessToken(token, expires_at)


class AuthenticationError(Exception):
    """Raised by ``decode_access_token`` — translated into the contract's
    Problem envelope by the ``auth_exception_handler`` registered in
    ``host/main.py``, never a bare 401 with no body."""


def decode_access_token(token: str, options: JwtOptions) -> dict:
    try:
        return jwt.decode(
            token,
            options.signing_key,
            algorithms=["HS256"],
            issuer=options.issuer,
            audience=options.audience,
            leeway=0,
        )
    except jwt.PyJWTError as exc:
        raise AuthenticationError(str(exc)) from exc


def get_jwt_options(request: Request) -> JwtOptions:
    return request.app.state.jwt_options  # type: ignore[no-any-return]


def get_current_user_id(request: Request, jwt_options: JwtOptions = Depends(get_jwt_options)) -> UUID:
    authorization = request.headers.get("authorization", "")
    if not authorization.lower().startswith("bearer "):
        raise AuthenticationError("Missing bearer token.")

    raw_token = authorization[len("bearer ") :].strip()
    claims = decode_access_token(raw_token, jwt_options)

    try:
        return UUID(claims["sub"])
    except (KeyError, ValueError) as exc:
        raise AuthenticationError("Token does not carry a valid user id.") from exc


def get_current_permissions(request: Request, jwt_options: JwtOptions = Depends(get_jwt_options)) -> list[str]:
    authorization = request.headers.get("authorization", "")
    if not authorization.lower().startswith("bearer "):
        raise AuthenticationError("Missing bearer token.")

    raw_token = authorization[len("bearer ") :].strip()
    claims = decode_access_token(raw_token, jwt_options)
    return list(claims.get(PERMISSION_CLAIM, []))


def require_permission(permission: str):
    """A FastAPI dependency factory: an endpoint names the permission code
    it needs, and the caller's access token must carry it — see
    ``contract/openapi.yaml``'s per-endpoint 403 responses.
    """

    def dependency(
        user_id: UUID = Depends(get_current_user_id),
        permissions: list[str] = Depends(get_current_permissions),
    ) -> UUID:
        if permission not in permissions:
            raise ForbiddenError(user_id)
        return user_id

    return dependency


class ForbiddenError(Exception):
    def __init__(self, user_id: UUID) -> None:
        super().__init__(f"User {user_id} lacks the required permission.")
        self.user_id = user_id


def app_error_for_auth_failure() -> AppError:
    return AppError.unauthorized("IDENTITY.UNAUTHORIZED", "identity.unauthorized")


def app_error_for_forbidden() -> AppError:
    return AppError.forbidden("IDENTITY.FORBIDDEN", "identity.forbidden")
