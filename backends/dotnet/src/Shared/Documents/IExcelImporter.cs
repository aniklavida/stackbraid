namespace StackBraid.Shared.Documents;

/// <summary>
/// Imports tabular data from an Excel workbook, collecting all row-level validation
/// errors across the entire document without stopping at the first failure.
/// </summary>
public interface IExcelImporter
{
    ExcelImportResult<T> Import<T>(
        Stream stream,
        Func<IExcelRowReader, T?> rowMapper,
        string? sheetName = null);
}
