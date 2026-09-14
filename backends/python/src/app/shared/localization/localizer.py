"""Resolves a message key (never a hard-coded sentence) into human text for a
given culture. ``Problem.code`` in the contract stays stable across locales —
this is what produces the localized ``title``/``detail`` text that sits next
to it.

Backed by one implementation (``JsonAppLocalizer``, loading flat key -> text
JSON files bundled under ``resources/``); a feature depends only on the
``AppLocalizer`` protocol, never on how the strings are stored.
"""

from __future__ import annotations

import json
from functools import lru_cache
from pathlib import Path
from typing import Mapping, Protocol, Sequence

from app.shared.localization.accept_language import negotiate_accept_language


class AppLocalizer(Protocol):
    def get_string(
        self,
        key: str,
        culture: str | None = None,
        arguments: Mapping[str, str] | None = None,
    ) -> str: ...

    @property
    def supported_cultures(self) -> Sequence[str]: ...


class JsonAppLocalizer:
    DEFAULT_CULTURE = "en"
    _SUPPORTED = ("en", "es")
    _RESOURCES_DIR = Path(__file__).parent / "resources"

    @property
    def supported_cultures(self) -> Sequence[str]:
        return self._SUPPORTED

    def get_string(
        self,
        key: str,
        culture: str | None = None,
        arguments: Mapping[str, str] | None = None,
    ) -> str:
        resolved = self._normalize(culture)
        text = self._load(resolved).get(key) or self._load(self.DEFAULT_CULTURE).get(key) or key

        if not arguments:
            return text

        for name, value in arguments.items():
            text = text.replace("{" + name + "}", value)
        return text

    def _normalize(self, culture: str | None) -> str:
        if not culture:
            return self.DEFAULT_CULTURE
        return negotiate_accept_language(culture, self._SUPPORTED, self.DEFAULT_CULTURE)

    @lru_cache(maxsize=None)
    def _load(self, culture: str) -> Mapping[str, str]:
        path = self._RESOURCES_DIR / f"{culture}.json"
        if not path.exists():
            raise RuntimeError(f"Missing localization resource file '{path}'.")
        with path.open(encoding="utf-8") as handle:
            return json.load(handle)
