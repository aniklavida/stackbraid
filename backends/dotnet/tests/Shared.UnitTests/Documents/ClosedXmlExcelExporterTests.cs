using ClosedXML.Excel;
using Shouldly;
using StackBraid.Shared.Documents;

namespace StackBraid.Shared.UnitTests.Documents;

public sealed class ClosedXmlExcelExporterTests
{
    [Fact]
    public void ClosedXmlExcelExporter_exports_valid_xlsx_workbook()
    {
        var exporter = new ClosedXmlExcelExporter();
        var headers = new[] { "ID", "Name", "Role" };
        var rows = new[]
        {
            new[] { "1", "Alice", "Admin" },
            new[] { "2", "Bob", "User" },
        };

        var request = new ExcelExportRequest(headers, rows);
        var bytes = exporter.Export(request);

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);

        var sheet = workbook.Worksheets.FirstOrDefault(w => w.Name == "Export");
        sheet.ShouldNotBeNull();

        sheet.Cell(1, 1).GetString().ShouldBe("ID");
        sheet.Cell(1, 2).GetString().ShouldBe("Name");
        sheet.Cell(1, 3).GetString().ShouldBe("Role");
        sheet.Cell(1, 1).Style.Font.Bold.ShouldBeTrue();

        sheet.Cell(2, 1).GetString().ShouldBe("1");
        sheet.Cell(2, 2).GetString().ShouldBe("Alice");
        sheet.Cell(2, 3).GetString().ShouldBe("Admin");

        sheet.Cell(3, 1).GetString().ShouldBe("2");
        sheet.Cell(3, 2).GetString().ShouldBe("Bob");
        sheet.Cell(3, 3).GetString().ShouldBe("User");
    }

    [Fact]
    public void IExcelExporter_swappable_between_ClosedXml_and_Csv()
    {
        var headers = new[] { "Name", "Score" };
        var rows = new[] { new[] { "Ada", "100" } };
        var request = new ExcelExportRequest(headers, rows);

        IExcelExporter closedXml = new ClosedXmlExcelExporter();
        IExcelExporter csv = new CsvExcelExporter();

        var xlsxBytes = closedXml.Export(request);
        var csvBytes = csv.Export(request);

        xlsxBytes.Length.ShouldBeGreaterThan(0);
        csvBytes.Length.ShouldBeGreaterThan(0);

        // CSV starts with UTF-8 BOM followed by Name,Score
        var csvText = System.Text.Encoding.UTF8.GetString(csvBytes);
        csvText.ShouldContain("Name,Score");
        csvText.ShouldContain("Ada,100");
    }
}
