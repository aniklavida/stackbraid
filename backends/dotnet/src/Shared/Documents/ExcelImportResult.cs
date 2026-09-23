namespace StackBraid.Shared.Documents;

/// <summary>
/// Structured result of an Excel import operation containing all successful rows and accumulated row-level errors.
/// </summary>
public sealed record ExcelImportResult<T>(
    IReadOnlyList<T> SuccessfulRows,
    IReadOnlyList<ExcelRowError> Errors)
{
    public bool IsSuccess => Errors.Count == 0;
}
