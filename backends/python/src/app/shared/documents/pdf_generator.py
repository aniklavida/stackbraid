"""Hand-writes a valid PDF 1.4 document directly — one Catalog, one Pages
tree, one Page per ``LINES_PER_PAGE`` lines of the built-in Helvetica font,
and a correct cross-reference table. No compression, no embedded fonts, no
images: exactly enough of the PDF specification to turn plain text into a
document any standard reader opens.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol, Sequence

_LINES_PER_PAGE = 50
_PAGE_WIDTH = 612  # US Letter, in points
_PAGE_HEIGHT = 792
_LEFT_MARGIN = 56
_TOP_MARGIN = 740
_LINE_HEIGHT = 14


@dataclass(frozen=True, slots=True)
class PdfDocumentRequest:
    title: str
    lines: Sequence[str]


class PdfGenerator(Protocol):
    def generate(self, request: PdfDocumentRequest) -> bytes: ...


class MinimalPdfGenerator:
    def generate(self, request: PdfDocumentRequest) -> bytes:
        pages = _paginate(request.lines)
        writer = _PdfObjectWriter()

        font_id = writer.reserve()
        catalog_id = writer.reserve()
        pages_id = writer.reserve()
        page_ids = [writer.reserve() for _ in pages]
        content_ids = [writer.reserve() for _ in pages]

        writer.write_object(font_id, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>")

        kids = " ".join(f"{pid} 0 R" for pid in page_ids)
        writer.write_object(pages_id, f"<< /Type /Pages /Kids [{kids}] /Count {len(page_ids)} >>")
        writer.write_object(catalog_id, f"<< /Type /Catalog /Pages {pages_id} 0 R >>")

        for i, page_lines in enumerate(pages):
            writer.write_object(
                page_ids[i],
                f"<< /Type /Page /Parent {pages_id} 0 R /MediaBox [0 0 {_PAGE_WIDTH} {_PAGE_HEIGHT}] "
                f"/Resources << /Font << /F1 {font_id} 0 R >> >> /Contents {content_ids[i]} 0 R >>",
            )
            content = _build_page_content(request.title, page_lines, i == 0)
            content_len = len(content.encode("ascii"))
            writer.write_object(content_ids[i], f"<< /Length {content_len} >>\nstream\n{content}\nendstream")

        return writer.build(catalog_id)


def _paginate(lines: Sequence[str]) -> list[list[str]]:
    pages = [list(lines[i : i + _LINES_PER_PAGE]) for i in range(0, len(lines), _LINES_PER_PAGE)]
    return pages or [[]]


def _build_page_content(title: str, lines: Sequence[str], is_first_page: bool) -> str:
    parts = ["BT /F1 12 Tf "]
    y = _TOP_MARGIN

    if is_first_page:
        parts.append(f"{_LEFT_MARGIN} {y} Td ({_escape(title)}) Tj ")
        parts.append(f"0 {-(_LINE_HEIGHT * 2)} Td ")
    else:
        parts.append(f"{_LEFT_MARGIN} {y} Td ")

    for line in lines:
        parts.append(f"({_escape(line)}) Tj 0 {-_LINE_HEIGHT} Td ")

    parts.append("ET")
    return "".join(parts)


def _escape(text: str) -> str:
    return text.replace("\\", "\\\\").replace("(", "\\(").replace(")", "\\)")


class _PdfObjectWriter:
    """Tracks object offsets so the trailing cross-reference table is
    byte-accurate."""

    def __init__(self) -> None:
        self._objects: list[str | None] = []
        self._buffer = bytearray()

    def reserve(self) -> int:
        self._objects.append(None)
        return len(self._objects)  # PDF object numbers are 1-based.

    def write_object(self, obj_id: int, body: str) -> None:
        self._objects[obj_id - 1] = body

    def build(self, catalog_id: int) -> bytes:
        self._buffer.extend(b"%PDF-1.4\n")

        offsets = [0] * (len(self._objects) + 1)
        for i, body in enumerate(self._objects):
            offsets[i + 1] = len(self._buffer)
            if body is None:
                raise ValueError(f"PDF object {i + 1} was reserved but never written.")
            self._buffer.extend(f"{i + 1} 0 obj\n{body}\nendobj\n".encode("ascii"))

        xref_offset = len(self._buffer)
        xref = [f"xref\n0 {len(self._objects) + 1}\n", "0000000000 65535 f \n"]
        xref.extend(f"{offsets[i]:010d} 00000 n \n" for i in range(1, len(self._objects) + 1))
        self._buffer.extend("".join(xref).encode("ascii"))
        self._buffer.extend(
            f"trailer\n<< /Size {len(self._objects) + 1} /Root {catalog_id} 0 R >>\nstartxref\n{xref_offset}\n%%EOF".encode(
                "ascii"
            )
        )

        return bytes(self._buffer)
