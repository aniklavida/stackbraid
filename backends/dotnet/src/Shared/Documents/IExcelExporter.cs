namespace StackBraid.Shared.Documents;

public sealed record ExcelExportRequest(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>
/// Exports tabular data for a spreadsheet application to open. One
/// implementation ships today, <see cref="CsvExcelExporter"/> — genuine,
/// spec-compliant CSV (RFC 4180 quoting), not a stub, and every spreadsheet
/// application opens it directly. A native <c>.xlsx</c> writer (ClosedXML —
/// see <c>docs/SPEC.md</c>'s dependency table) is planned as a second
/// implementation behind this same interface, for row-level import error
/// reporting and styled export.
/// </summary>
public interface IExcelExporter
{
    byte[] Export(ExcelExportRequest request);
}
