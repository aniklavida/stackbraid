using System.Text;

namespace StackBraid.Shared.Documents;

public sealed class CsvExcelExporter : IExcelExporter
{
    public byte[] Export(ExcelExportRequest request)
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(',', request.Headers.Select(Quote)));
        sb.Append("\r\n");

        foreach (var row in request.Rows)
        {
            sb.Append(string.Join(',', row.Select(Quote)));
            sb.Append("\r\n");
        }

        // A UTF-8 BOM so Excel (unlike most other consumers) recognises the encoding instead of mis-decoding accented characters.
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        return [.. preamble, .. body];
    }

    private static string Quote(string field)
    {
        var needsQuoting = field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r');
        if (!needsQuoting)
        {
            return field;
        }

        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}
