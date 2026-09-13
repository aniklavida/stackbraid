namespace StackBraid.Shared.Documents;

public sealed record PdfDocumentRequest(string Title, IReadOnlyList<string> Lines);

/// <summary>
/// Renders plain text into a PDF. One implementation ships today,
/// <see cref="MinimalPdfGenerator"/> — a small, hand-written writer that
/// emits valid single-font text pages directly in PDF syntax, with no
/// third-party dependency. It has no support for images, tables or custom
/// fonts; a richer generator (QuestPDF — see <c>docs/SPEC.md</c>'s
/// dependency table) is planned as a second implementation behind this
/// same interface, for when a real document needs those.
/// </summary>
public interface IPdfGenerator
{
    byte[] Generate(PdfDocumentRequest request);
}
