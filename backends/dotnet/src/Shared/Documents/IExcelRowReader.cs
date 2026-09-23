namespace StackBraid.Shared.Documents;

/// <summary>
/// Provides typed extraction and validation for a single row during Excel import.
/// Collects all column-level errors without stopping at the first failure.
/// </summary>
public interface IExcelRowReader
{
    /// <summary>1-based row number in the spreadsheet.</summary>
    int RowNumber { get; }

    /// <summary>True if every cell on the row is blank.</summary>
    bool IsEmpty { get; }

    /// <summary>True if any validation errors were registered for this row.</summary>
    bool HasErrors { get; }

    /// <summary>All validation errors recorded for this row.</summary>
    IReadOnlyList<ExcelRowError> Errors { get; }

    /// <summary>Reads raw trimmed string value or null if empty.</summary>
    string? GetString(string column);

    /// <summary>Requires non-empty string; adds an error if missing.</summary>
    string RequireString(string column, string? reason = null);

    /// <summary>Optional string; returns null if empty.</summary>
    string? OptionalString(string column);

    /// <summary>Requires valid email address; adds an error if missing or invalid.</summary>
    string RequireEmail(string column, string? reason = null);

    /// <summary>Optional email address; adds an error only if non-empty but invalid format.</summary>
    string? OptionalEmail(string column, string? reason = null);

    /// <summary>Requires 32-bit integer; adds an error if missing or not an integer.</summary>
    int RequireInt(string column, string? reason = null);

    /// <summary>Optional integer; adds an error only if non-empty but not an integer.</summary>
    int? OptionalInt(string column, string? reason = null);

    /// <summary>Requires decimal number; adds an error if missing or invalid.</summary>
    decimal RequireDecimal(string column, string? reason = null);

    /// <summary>Optional decimal; adds an error only if non-empty but invalid.</summary>
    decimal? OptionalDecimal(string column, string? reason = null);

    /// <summary>Requires DateTime; adds an error if missing or invalid format.</summary>
    DateTime RequireDateTime(string column, string? reason = null);

    /// <summary>Optional DateTime; adds an error only if non-empty but invalid format.</summary>
    DateTime? OptionalDateTime(string column, string? reason = null);

    /// <summary>Requires boolean; adds an error if missing or invalid.</summary>
    bool RequireBoolean(string column, string? reason = null);

    /// <summary>Optional boolean; adds an error only if non-empty but invalid.</summary>
    bool? OptionalBoolean(string column, string? reason = null);

    /// <summary>Adds a custom validation error to this row.</summary>
    void AddError(string column, string reason);
}
