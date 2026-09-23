using ClosedXML.Excel;

namespace StackBraid.Shared.Documents;

/// <summary>
/// Exports tabular data as native OpenXML (.xlsx) workbooks using ClosedXML (MIT licensed).
/// </summary>
public sealed class ClosedXmlExcelExporter : IExcelExporter
{
    public byte[] Export(ExcelExportRequest request)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Export");

        // Write header row with bold formatting and light background styling
        for (var col = 0; col < request.Headers.Count; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = request.Headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F4F8");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#D0D5DD");
        }

        // Write data rows
        for (var row = 0; row < request.Rows.Count; row++)
        {
            var rowData = request.Rows[row];
            for (var col = 0; col < rowData.Count; col++)
            {
                worksheet.Cell(row + 2, col + 1).Value = rowData[col];
            }
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
