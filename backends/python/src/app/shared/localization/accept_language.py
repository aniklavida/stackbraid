"""Parses an ``Accept-Language`` header value into the single supported
culture that best matches it. This is the one negotiation rule this backend
shares with the .NET backend's
``StackBraid.Shared.Localization.AcceptLanguageNegotiator`` — same parsing
steps, same precedence, so the two backends resolve an identical header to
an identical culture.
"""

from __future__ import annotations

from typing import Sequence


def negotiate_accept_language(header: str, supported_cultures: Sequence[str], default_culture: str) -> str:
    """``header`` is a comma-separated list of language ranges, each
    optionally carrying a ``;q=`` weight (RFC 9110 §12.5.4). A range with no
    weight defaults to 1.0, and a weight that fails to parse is treated the
    same way rather than dropping the range — a slightly malformed header
    should still degrade to "pick something reasonable", not "ignore the
    whole header". Ranges are ordered by weight, highest first, keeping the
    header's own order for ties (a stable sort). Each range's primary
    subtag ("es-MX" -> "es") is matched against ``supported_cultures``; the
    first match wins. No match anywhere returns ``default_culture``.
    """
    ranges: list[tuple[float, int, str]] = []
    for index, raw in enumerate(header.split(",")):
        parsed = _parse_range(raw)
        if parsed is None:
            continue
        primary, weight = parsed
        ranges.append((weight, index, primary))

    # Highest weight first; the index keeps ties in the header's own order
    # (a plain descending sort on weight alone would leave ties in
    # arbitrary/reversed order).
    for _, _, primary in sorted(ranges, key=lambda r: (-r[0], r[1])):
        if primary in supported_cultures:
            return primary

    return default_culture


def _parse_range(raw: str) -> tuple[str, float] | None:
    parts = raw.split(";")
    tag = parts[0].strip()
    if not tag:
        return None

    weight = 1.0
    for param in parts[1:]:
        param = param.strip()
        if not param.lower().startswith("q="):
            continue
        try:
            weight = float(param[2:])
        except ValueError:
            pass
        break

    primary = tag.split("-")[0].strip().lower()
    return primary, weight
