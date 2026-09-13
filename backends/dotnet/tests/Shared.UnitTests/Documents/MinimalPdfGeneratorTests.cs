using System.Text;
using Shouldly;
using StackBraid.Shared.Documents;

namespace StackBraid.Shared.UnitTests.Documents;

public class MinimalPdfGeneratorTests
{
    private readonly MinimalPdfGenerator _sut = new();

    [Fact]
    public void Generate_produces_bytes_starting_with_the_pdf_header()
    {
        var bytes = _sut.Generate(new PdfDocumentRequest("Report", ["line one"]));

        Encoding.ASCII.GetString(bytes, 0, 8).ShouldBe("%PDF-1.4");
    }

    [Fact]
    public void Generate_produces_bytes_ending_with_the_eof_marker()
    {
        var bytes = _sut.Generate(new PdfDocumentRequest("Report", ["line one"]));

        Encoding.ASCII.GetString(bytes).TrimEnd().ShouldEndWith("%%EOF");
    }

    [Fact]
    public void Generate_embeds_the_title_and_every_line_as_literal_pdf_text()
    {
        var bytes = _sut.Generate(new PdfDocumentRequest("Monthly Report", ["Alpha", "Beta"]));
        var text = Encoding.ASCII.GetString(bytes);

        text.ShouldContain("(Monthly Report) Tj");
        text.ShouldContain("(Alpha) Tj");
        text.ShouldContain("(Beta) Tj");
    }

    [Fact]
    public void Generate_escapes_parentheses_so_the_content_stream_stays_valid()
    {
        var bytes = _sut.Generate(new PdfDocumentRequest("Title", ["a (b) c"]));
        var text = Encoding.ASCII.GetString(bytes);

        text.ShouldContain(@"a \(b\) c");
    }

    [Fact]
    public void Generate_splits_more_than_fifty_lines_across_multiple_pages()
    {
        var lines = Enumerable.Range(1, 120).Select(i => $"line {i}").ToArray();

        var bytes = _sut.Generate(new PdfDocumentRequest("Big report", lines));
        var text = Encoding.ASCII.GetString(bytes);

        // Three pages of up to 50 lines each for 120 lines — the Pages node names every Page object.
        text.ShouldContain("/Type /Pages");
        text.Split("/Type /Page ").Length.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void Generate_writes_a_cross_reference_entry_for_every_object()
    {
        var bytes = _sut.Generate(new PdfDocumentRequest("Title", ["one line"]));
        var text = Encoding.ASCII.GetString(bytes);

        // Catalog, Pages, one Page, Font and one content stream — 5 objects for a single-page document.
        var xrefSection = text[text.IndexOf("\nxref\n", StringComparison.Ordinal)..];
        xrefSection.ShouldContain("0000000000 65535 f");
    }
}
