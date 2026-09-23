using System.Text;
using Shouldly;
using StackBraid.Shared.Documents;

namespace StackBraid.Shared.UnitTests.Documents;

public sealed class QuestPdfGeneratorTests
{
    [Fact]
    public void QuestPdfGenerator_generates_valid_pdf_document()
    {
        var generator = new QuestPdfGenerator();
        var request = new PdfDocumentRequest(
            "Monthly Statement",
            ["Account: 12345", "Total: $1,250.00", "Status: Paid"]);

        var bytes = generator.Generate(request);

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);

        var header = Encoding.ASCII.GetString(bytes.Take(5).ToArray());
        header.ShouldBe("%PDF-");
    }

    [Fact]
    public void IPdfGenerator_swappable_between_QuestPdf_and_MinimalPdf()
    {
        var request = new PdfDocumentRequest("Invoice", ["Item 1: $10", "Item 2: $20"]);

        IPdfGenerator questPdf = new QuestPdfGenerator();
        IPdfGenerator minimalPdf = new MinimalPdfGenerator();

        var questBytes = questPdf.Generate(request);
        var minimalBytes = minimalPdf.Generate(request);

        questBytes.Length.ShouldBeGreaterThan(0);
        minimalBytes.Length.ShouldBeGreaterThan(0);

        Encoding.ASCII.GetString(questBytes.Take(5).ToArray()).ShouldBe("%PDF-");
        Encoding.ASCII.GetString(minimalBytes.Take(5).ToArray()).ShouldBe("%PDF-");
    }
}
