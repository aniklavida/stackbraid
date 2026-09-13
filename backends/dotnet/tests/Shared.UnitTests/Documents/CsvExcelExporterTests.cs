using System.Text;
using Shouldly;
using StackBraid.Shared.Documents;

namespace StackBraid.Shared.UnitTests.Documents;

public class CsvExcelExporterTests
{
    private readonly CsvExcelExporter _sut = new();

    private static string WithoutBom(byte[] bytes)
    {
        var preambleLength = Encoding.UTF8.GetPreamble().Length;
        return Encoding.UTF8.GetString(bytes, preambleLength, bytes.Length - preambleLength);
    }

    [Fact]
    public void Export_writes_a_utf8_bom_so_excel_reads_accents_correctly()
    {
        var bytes = _sut.Export(new ExcelExportRequest(["Name"], [["José"]]));

        bytes.Take(3).ToArray().ShouldBe(Encoding.UTF8.GetPreamble());
    }

    [Fact]
    public void Export_writes_the_header_row_then_each_data_row()
    {
        var bytes = _sut.Export(new ExcelExportRequest(
            ["Email", "DisplayName"],
            [["a@example.com", "Ada"], ["b@example.com", "Bea"]]));

        var text = WithoutBom(bytes);
        text.ShouldBe("Email,DisplayName\r\na@example.com,Ada\r\nb@example.com,Bea\r\n");
    }

    [Fact]
    public void Export_quotes_a_field_containing_a_comma()
    {
        var bytes = _sut.Export(new ExcelExportRequest(["Name"], [["Smith, Jr."]]));

        var text = WithoutBom(bytes);
        text.ShouldContain("\"Smith, Jr.\"");
    }

    [Fact]
    public void Export_escapes_an_embedded_quote_by_doubling_it()
    {
        var bytes = _sut.Export(new ExcelExportRequest(["Name"], [["Say \"hi\""]]));

        var text = WithoutBom(bytes);
        text.ShouldContain("\"Say \"\"hi\"\"\"");
    }
}
