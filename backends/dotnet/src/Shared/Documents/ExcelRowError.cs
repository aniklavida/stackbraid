namespace StackBraid.Shared.Documents;

/// <summary>
/// Represents a validation error encountered on a specific row and column of a spreadsheet.
/// </summary>
public sealed record ExcelRowError(int RowNumber, string Column, string Reason)
{
    public string FormattedMessage => $"Row {RowNumber}, column {Column}: {Reason}";

    public override string ToString() => FormattedMessage;
}
