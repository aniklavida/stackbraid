using ClosedXML.Excel;
using Shouldly;
using StackBraid.Shared.Documents;

namespace StackBraid.Shared.UnitTests.Documents;

public sealed record UserImportDto(string Name, string Email, int Age);

public sealed class ExcelImporterTests
{
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "malformed_users.xlsx");

    static ExcelImporterTests()
    {
        EnsureFixtureCreated();
    }

    private static void EnsureFixtureCreated()
    {
        var fixtureDir = Path.GetDirectoryName(FixturePath)!;
        Directory.CreateDirectory(fixtureDir);

        if (!File.Exists(FixturePath))
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Users");

            // Row 1: Header
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Email";
            ws.Cell(1, 3).Value = "Age";

            // Row 2: Valid row
            ws.Cell(2, 1).Value = "Alice";
            ws.Cell(2, 2).Value = "alice@example.com";
            ws.Cell(2, 3).Value = 30;

            // Row 3: Bad email format
            ws.Cell(3, 1).Value = "Bob";
            ws.Cell(3, 2).Value = "not-an-email";
            ws.Cell(3, 3).Value = 25;

            // Row 4: Missing required field (Name is blank)
            ws.Cell(4, 1).Value = "";
            ws.Cell(4, 2).Value = "carol@example.com";
            ws.Cell(4, 3).Value = 28;

            // Row 5: Wrong data type (Age is text instead of int)
            ws.Cell(5, 1).Value = "Dave";
            ws.Cell(5, 2).Value = "dave@example.com";
            ws.Cell(5, 3).Value = "not-a-number";

            // Row 6: Multiple errors on single row (Missing name, bad email, wrong age type)
            ws.Cell(6, 1).Value = "";
            ws.Cell(6, 2).Value = "invalid-email-address";
            ws.Cell(6, 3).Value = "wrong-data-type";

            // Row 42: Bad email format specifically on row 42
            ws.Cell(42, 1).Value = "Eve";
            ws.Cell(42, 2).Value = "not a valid address";
            ws.Cell(42, 3).Value = 42;

            workbook.SaveAs(FixturePath);
        }
    }

    [Fact]
    public void Malformed_spreadsheet_fixture_produces_structured_readable_error_list()
    {
        // Arrange
        var importer = new ClosedXmlExcelImporter();
        using var stream = File.OpenRead(FixturePath);

        // Act: Parse the whole file and accumulate every row-level error
        var result = importer.Import<UserImportDto>(stream, row =>
        {
            var name = row.RequireString("Name", "required field is missing");
            var email = row.RequireEmail("Email", "not a valid address");
            var age = row.RequireInt("Age", "invalid integer format (expected integer)");

            if (row.HasErrors)
            {
                return null;
            }

            return new UserImportDto(name, email, age);
        });

        // Assert: Whole file was processed without stopping at the first error
        result.IsSuccess.ShouldBeFalse();
        result.SuccessfulRows.Count.ShouldBe(1);
        result.SuccessfulRows[0].Name.ShouldBe("Alice");
        result.SuccessfulRows[0].Email.ShouldBe("alice@example.com");
        result.SuccessfulRows[0].Age.ShouldBe(30);

        // Collect all readable error strings
        var errorMessages = result.Errors.Select(e => e.ToString()).ToList();

        // 1. Bad email format on row 3
        result.Errors.ShouldContain(e => e.RowNumber == 3 && e.Column == "Email" && e.Reason == "not a valid address");
        errorMessages.ShouldContain("Row 3, column Email: not a valid address");

        // 2. Missing required field on row 4
        result.Errors.ShouldContain(e => e.RowNumber == 4 && e.Column == "Name" && e.Reason == "required field is missing");
        errorMessages.ShouldContain("Row 4, column Name: required field is missing");

        // 3. Wrong data type on row 5
        result.Errors.ShouldContain(e => e.RowNumber == 5 && e.Column == "Age" && e.Reason == "invalid integer format (expected integer)");
        errorMessages.ShouldContain("Row 5, column Age: invalid integer format (expected integer)");

        // 4. Multiple errors collected for row 6
        result.Errors.ShouldContain(e => e.RowNumber == 6 && e.Column == "Name");
        result.Errors.ShouldContain(e => e.RowNumber == 6 && e.Column == "Email");
        result.Errors.ShouldContain(e => e.RowNumber == 6 && e.Column == "Age");

        // 5. Row 42 error matches the exact phrase: "Row 42, column Email: not a valid address"
        result.Errors.ShouldContain(e => e.RowNumber == 42 && e.Column == "Email" && e.Reason == "not a valid address");
        errorMessages.ShouldContain("Row 42, column Email: not a valid address");

        // Proven: all errors from all rows collected
        result.Errors.Count.ShouldBe(7);
    }

    [Fact]
    public void Valid_spreadsheet_imports_successfully_without_errors()
    {
        var importer = new ClosedXmlExcelImporter();
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Users");
        ws.Cell(1, 1).Value = "Name";
        ws.Cell(1, 2).Value = "Email";
        ws.Cell(1, 3).Value = "Age";

        ws.Cell(2, 1).Value = "Alice";
        ws.Cell(2, 2).Value = "alice@example.com";
        ws.Cell(2, 3).Value = 29;

        ws.Cell(3, 1).Value = "Bob";
        ws.Cell(3, 2).Value = "bob@example.com";
        ws.Cell(3, 3).Value = 35;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var result = importer.Import<UserImportDto>(ms, row =>
        {
            var name = row.RequireString("Name");
            var email = row.RequireEmail("Email");
            var age = row.RequireInt("Age");
            return row.HasErrors ? null : new UserImportDto(name, email, age);
        });

        result.IsSuccess.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.SuccessfulRows.Count.ShouldBe(2);
        result.SuccessfulRows[0].Name.ShouldBe("Alice");
        result.SuccessfulRows[1].Name.ShouldBe("Bob");
    }

    [Fact]
    public void Optional_fields_allow_empty_cells_but_validate_non_empty_values()
    {
        var importer = new ClosedXmlExcelImporter();
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Users");
        ws.Cell(1, 1).Value = "Name";
        ws.Cell(1, 2).Value = "Email";
        ws.Cell(1, 3).Value = "Age";

        // Row 2: optional email empty -> valid
        ws.Cell(2, 1).Value = "Alice";
        ws.Cell(2, 2).Value = "";
        ws.Cell(2, 3).Value = 30;

        // Row 3: optional email has invalid format -> error
        ws.Cell(3, 1).Value = "Bob";
        ws.Cell(3, 2).Value = "bad-email";
        ws.Cell(3, 3).Value = 30;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var result = importer.Import<UserImportDto>(ms, row =>
        {
            var name = row.RequireString("Name");
            var email = row.OptionalEmail("Email") ?? "default@example.com";
            var age = row.OptionalInt("Age") ?? 0;
            return row.HasErrors ? null : new UserImportDto(name, email, age);
        });

        result.SuccessfulRows.Count.ShouldBe(1);
        result.SuccessfulRows[0].Name.ShouldBe("Alice");
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].RowNumber.ShouldBe(3);
        result.Errors[0].Column.ShouldBe("Email");
    }
}
