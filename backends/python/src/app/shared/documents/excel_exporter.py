"""A dependency-free CSV exporter behind the same port a real Excel writer
would implement — a spreadsheet application opens a CSV directly, and this
skeleton needs row-level export, not Excel's binary format specifically.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol, Sequence


@dataclass(frozen=True, slots=True)
class ExcelExportRequest:
    headers: Sequence[str]
    rows: Sequence[Sequence[str]]


class ExcelExporter(Protocol):
    def export(self, request: ExcelExportRequest) -> bytes: ...


class CsvExcelExporter:
    def export(self, request: ExcelExportRequest) -> bytes:
        lines = [_join(request.headers)]
        lines.extend(_join(row) for row in request.rows)
        # A UTF-8 BOM so Excel (unlike most other consumers) recognises the
        # encoding instead of mis-decoding accented characters.
        return b"\xef\xbb\xbf" + "\r\n".join(lines).encode("utf-8") + b"\r\n"


def _join(fields: Sequence[str]) -> str:
    return ",".join(_quote(field) for field in fields)


def _quote(field: str) -> str:
    if not any(ch in field for ch in (",", '"', "\n", "\r")):
        return field
    return '"' + field.replace('"', '""') + '"'
