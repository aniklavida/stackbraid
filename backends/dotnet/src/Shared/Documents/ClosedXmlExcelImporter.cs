using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace StackBraid.Shared.Documents;

public sealed partial class ClosedXmlExcelImporter : IExcelImporter
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ExcelImportResult<T> Import<T>(
        Stream stream,
        Func<IExcelRowReader, T?> rowMapper,
        string? sheetName = null)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = string.IsNullOrWhiteSpace(sheetName)
            ? workbook.Worksheets.FirstOrDefault()
            : workbook.Worksheets.FirstOrDefault(w => string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase));

        if (worksheet is null)
        {
            return new ExcelImportResult<T>([], [new ExcelRowError(0, "Worksheet", "Worksheet not found.")]);
        }

        var headerRow = worksheet.Row(1);
        if (headerRow is null || headerRow.IsEmpty())
        {
            return new ExcelImportResult<T>([], [new ExcelRowError(1, "Header", "Header row is missing or empty.")]);
        }

        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCell = headerRow.LastCellUsed();
        var lastCol = lastCell?.Address.ColumnNumber ?? 0;

        for (var col = 1; col <= lastCol; col++)
        {
            var headerValue = headerRow.Cell(col).GetString()?.Trim();
            if (!string.IsNullOrEmpty(headerValue))
            {
                columnMap[headerValue] = col;
            }
        }

        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var successfulRows = new List<T>();
        var allErrors = new List<ExcelRowError>();

        for (var rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var xlRow = worksheet.Row(rowNum);
            var reader = new ClosedXmlRowReader(rowNum, xlRow, columnMap);

            if (reader.IsEmpty)
            {
                continue;
            }

            var item = rowMapper(reader);

            if (reader.HasErrors)
            {
                allErrors.AddRange(reader.Errors);
            }
            else if (item is not null)
            {
                successfulRows.Add(item);
            }
        }

        return new ExcelImportResult<T>(successfulRows, allErrors);
    }

    private sealed class ClosedXmlRowReader : IExcelRowReader
    {
        private readonly IXLRow _row;
        private readonly IReadOnlyDictionary<string, int> _columnMap;
        private readonly List<ExcelRowError> _errors = [];

        public ClosedXmlRowReader(int rowNumber, IXLRow row, IReadOnlyDictionary<string, int> columnMap)
        {
            RowNumber = rowNumber;
            _row = row;
            _columnMap = columnMap;
        }

        public int RowNumber { get; }

        public bool IsEmpty => _row.IsEmpty();

        public bool HasErrors => _errors.Count > 0;

        public IReadOnlyList<ExcelRowError> Errors => _errors;

        public string? GetString(string column)
        {
            if (!_columnMap.TryGetValue(column, out var colIndex))
            {
                return null;
            }

            var cell = _row.Cell(colIndex);
            if (cell.IsEmpty())
            {
                return null;
            }

            var text = cell.GetFormattedString()?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        public string RequireString(string column, string? reason = null)
        {
            var value = GetString(column);
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(column, reason ?? "required field is missing");
                return string.Empty;
            }

            return value;
        }

        public string? OptionalString(string column) => GetString(column);

        public string RequireEmail(string column, string? reason = null)
        {
            var value = GetString(column);
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(column, reason ?? "required field is missing");
                return string.Empty;
            }

            if (!IsValidEmail(value))
            {
                AddError(column, reason ?? "not a valid address");
                return string.Empty;
            }

            return value;
        }

        public string? OptionalEmail(string column, string? reason = null)
        {
            var value = GetString(column);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!IsValidEmail(value))
            {
                AddError(column, reason ?? "not a valid address");
                return null;
            }

            return value;
        }

        public int RequireInt(string column, string? reason = null)
        {
            var value = GetString(column);
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(column, reason ?? "required field is missing");
                return 0;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) &&
                !int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result))
            {
                AddError(column, reason ?? "invalid integer format (expected integer)");
                return 0;
            }

            return result;
        }

        public int? OptionalInt(string column, string? reason = null)
        {
            var value = GetString(column);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) &&
                !int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result))
            {
                AddError(column, reason ?? "invalid integer format (expected integer)");
                return null;
            }

            return result;
        }

        public decimal RequireDecimal(string column, string? reason = null)
        {
            var value = GetString(column);
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(column, reason ?? "required field is missing");
                return 0m;
            }

            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) &&
                !decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result))
            {
                AddError(column, reason ?? "invalid decimal format");
                return 0m;
            }

            return result;
        }

        public decimal? OptionalDecimal(string column, string? reason = null)
        {
            var value = GetString(column);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) &&
                !decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result))
            {
                AddError(column, reason ?? "invalid decimal format");
                return null;
            }

            return result;
        }

        public DateTime RequireDateTime(string column, string? reason = null)
        {
            if (_columnMap.TryGetValue(column, out var colIndex))
            {
                var cell = _row.Cell(colIndex);
                if (cell.DataType == XLDataType.DateTime)
                {
                    return cell.GetDateTime();
                }
            }

            var value = GetString(column);
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(column, reason ?? "required field is missing");
                return default;
            }

            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) &&
                !DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
            {
                AddError(column, reason ?? "invalid date format");
                return default;
            }

            return result;
        }

        public DateTime? OptionalDateTime(string column, string? reason = null)
        {
            if (_columnMap.TryGetValue(column, out var colIndex))
            {
                var cell = _row.Cell(colIndex);
                if (cell.DataType == XLDataType.DateTime)
                {
                    return cell.GetDateTime();
                }
            }

            var value = GetString(column);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) &&
                !DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
            {
                AddError(column, reason ?? "invalid date format");
                return null;
            }

            return result;
        }

        public bool RequireBoolean(string column, string? reason = null)
        {
            if (_columnMap.TryGetValue(column, out var colIndex))
            {
                var cell = _row.Cell(colIndex);
                if (cell.DataType == XLDataType.Boolean)
                {
                    return cell.GetBoolean();
                }
            }

            var value = GetString(column);
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(column, reason ?? "required field is missing");
                return false;
            }

            if (!bool.TryParse(value, out var result))
            {
                AddError(column, reason ?? "invalid boolean format");
                return false;
            }

            return result;
        }

        public bool? OptionalBoolean(string column, string? reason = null)
        {
            if (_columnMap.TryGetValue(column, out var colIndex))
            {
                var cell = _row.Cell(colIndex);
                if (cell.DataType == XLDataType.Boolean)
                {
                    return cell.GetBoolean();
                }
            }

            var value = GetString(column);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!bool.TryParse(value, out var result))
            {
                AddError(column, reason ?? "invalid boolean format");
                return null;
            }

            return result;
        }

        public void AddError(string column, string reason)
        {
            _errors.Add(new ExcelRowError(RowNumber, column, reason));
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            if (!EmailRegex.IsMatch(email))
            {
                return false;
            }

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
