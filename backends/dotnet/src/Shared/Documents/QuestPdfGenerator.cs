using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace StackBraid.Shared.Documents;

/// <summary>
/// Generates rich PDF documents using QuestPDF under its Community license.
/// Designed behind <see cref="IPdfGenerator"/> so organizations requiring a different
/// license or engine can swap in <see cref="MinimalPdfGenerator"/> or a custom implementation.
/// </summary>
public sealed class QuestPdfGenerator : IPdfGenerator
{
    static QuestPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(PdfDocumentRequest request)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor("#1D2939"));

                page.Header()
                    .PaddingBottom(1, Unit.Centimetre)
                    .Row(row =>
                    {
                        row.RelativeItem().Text(request.Title).SemiBold().FontSize(18).FontColor("#101828");
                    });

                page.Content()
                    .PaddingVertical(0.5f, Unit.Centimetre)
                    .Column(col =>
                    {
                        col.Spacing(6);
                        foreach (var line in request.Lines)
                        {
                            col.Item().Text(line);
                        }
                    });

                page.Footer()
                    .PaddingTop(1, Unit.Centimetre)
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }
}
