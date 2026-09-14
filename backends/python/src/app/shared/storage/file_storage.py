"""Stores each blob as one file under a root directory. A key is sanitized to
a single path segment so it can never escape the root — no caller-supplied
path traverses outside the storage directory.
"""

from __future__ import annotations

import re
from pathlib import Path
from typing import BinaryIO, Protocol

_UNSAFE_CHARS = re.compile(r"[^A-Za-z0-9._-]+")


class FileStorage(Protocol):
    def save(self, key: str, content: bytes) -> None: ...

    def get(self, key: str) -> bytes | None: ...

    def delete(self, key: str) -> None: ...


class LocalFileStorage:
    def __init__(self, root_path: str = "storage") -> None:
        self._root = Path(root_path).resolve()
        self._root.mkdir(parents=True, exist_ok=True)

    def save(self, key: str, content: bytes) -> None:
        self._resolve(key).write_bytes(content)

    def get(self, key: str) -> bytes | None:
        path = self._resolve(key)
        return path.read_bytes() if path.exists() else None

    def delete(self, key: str) -> None:
        path = self._resolve(key)
        if path.exists():
            path.unlink()

    def _resolve(self, key: str) -> Path:
        safe_key = _UNSAFE_CHARS.sub("_", key)
        return self._root / safe_key
