"""PBKDF2-HMAC-SHA256, OWASP's current minimum (600,000 iterations, a
128-bit salt), built entirely on the standard library's ``hashlib`` — no
third-party dependency. The stored hash carries the iteration count and salt
alongside the derived key, so a future increase to the iteration count never
breaks verifying a password hashed under the old one.
"""

from __future__ import annotations

import hashlib
import hmac
import secrets
from base64 import b64decode, b64encode
from typing import Protocol

_SALT_SIZE_BYTES = 16
_KEY_SIZE_BYTES = 32
_ITERATIONS = 600_000
_ALGORITHM = "sha256"


class PasswordHasher(Protocol):
    def hash(self, password: str) -> str: ...

    def verify(self, password: str, hashed: str) -> bool: ...


class Pbkdf2PasswordHasher:
    def hash(self, password: str) -> str:
        salt = secrets.token_bytes(_SALT_SIZE_BYTES)
        key = hashlib.pbkdf2_hmac(_ALGORITHM, password.encode("utf-8"), salt, _ITERATIONS, _KEY_SIZE_BYTES)
        return f"pbkdf2-sha256${_ITERATIONS}${b64encode(salt).decode()}${b64encode(key).decode()}"

    def verify(self, password: str, hashed: str) -> bool:
        parts = hashed.split("$")
        if len(parts) != 4 or parts[0] != "pbkdf2-sha256":
            return False

        try:
            iterations = int(parts[1])
            salt = b64decode(parts[2])
            expected_key = b64decode(parts[3])
        except (ValueError, Exception):
            return False

        actual_key = hashlib.pbkdf2_hmac(_ALGORITHM, password.encode("utf-8"), salt, iterations, len(expected_key))
        return hmac.compare_digest(actual_key, expected_key)
