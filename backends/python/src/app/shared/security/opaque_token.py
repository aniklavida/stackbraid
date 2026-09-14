"""Generates and hashes opaque bearer tokens such as a refresh token: a
high-entropy random value handed to the caller once, with only its hash ever
persisted — a database read alone can never recover a usable token.

A fast, deterministic hash (SHA-256) is correct here, unlike password
hashing: the token is already high-entropy and looked up by hash on every
request, so a slow, deliberately expensive hash would only punish legitimate
traffic.
"""

from __future__ import annotations

import hashlib
import secrets

_TOKEN_SIZE_BYTES = 32


def generate_raw_token() -> str:
    return secrets.token_urlsafe(_TOKEN_SIZE_BYTES)


def hash_token(raw_token: str) -> str:
    return hashlib.sha256(raw_token.encode("utf-8")).hexdigest()
