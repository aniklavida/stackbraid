from app.shared.documents.excel_exporter import CsvExcelExporter, ExcelExportRequest
from app.shared.documents.pdf_generator import MinimalPdfGenerator, PdfDocumentRequest


def test_csv_exporter_quotes_fields_containing_commas() -> None:
    exporter = CsvExcelExporter()
    output = exporter.export(ExcelExportRequest(headers=["name", "note"], rows=[["Ada", "loves, semicolons"]]))

    text = output.decode("utf-8-sig")
    assert "name,note\r\n" in text
    assert '"loves, semicolons"' in text


def test_pdf_generator_produces_a_well_formed_document() -> None:
    generator = MinimalPdfGenerator()
    output = generator.generate(PdfDocumentRequest(title="Report", lines=["line one", "line two"]))

    assert output.startswith(b"%PDF-1.4")
    assert output.rstrip().endswith(b"%%EOF")
    assert b"xref" in output
