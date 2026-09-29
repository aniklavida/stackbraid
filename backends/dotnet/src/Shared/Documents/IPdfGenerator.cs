namespace StackBraid.Shared.Documents;

public sealed record PdfDocumentRequest(string Title, IReadOnlyList<string> Lines);

/// <summary>
/// Renders plain text into a PDF. One implementation ships today,
/// <see cref="MinimalPdfGenerator"/> — a small, hand-written writer that
/// emits valid single-font text pages directly in PDF syntax, with no
/// third-party dependency. It has no support for images, tables or custom
/// fonts, and it produces plainer output than a layout engine would.
/// The interface is the seam: a richer generator can be registered in its
/// place without touching any caller.
/// </summary>
public interface IPdfGenerator
{
    byte[] Generate(PdfDocumentRequest request);
}
