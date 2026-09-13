using System.Globalization;
using System.Text;

namespace StackBraid.Shared.Documents;

/// <summary>
/// Hand-writes a valid PDF 1.4 document directly — one Catalog, one Pages
/// tree, one Page per <see cref="LinesPerPage"/> lines of the built-in
/// Helvetica font, and a correct cross-reference table. No compression, no
/// embedded fonts, no images: exactly enough of the PDF specification to
/// turn plain text into a document any standard reader opens.
/// </summary>
public sealed class MinimalPdfGenerator : IPdfGenerator
{
    private const int LinesPerPage = 50;
    private const double PageWidth = 612; // US Letter, in points
    private const double PageHeight = 792;
    private const double LeftMargin = 56;
    private const double TopMargin = 740;
    private const double LineHeight = 14;

    public byte[] Generate(PdfDocumentRequest request)
    {
        var pages = Paginate(request.Lines);
        var writer = new PdfObjectWriter();

        var fontId = writer.Reserve();
        var catalogId = writer.Reserve();
        var pagesId = writer.Reserve();

        var pageIds = new List<int>();
        var contentIds = new List<int>();
        foreach (var _ in pages)
        {
            pageIds.Add(writer.Reserve());
            contentIds.Add(writer.Reserve());
        }

        writer.WriteObject(fontId, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var kids = string.Join(' ', pageIds.Select(id => $"{id} 0 R"));
        writer.WriteObject(pagesId, $"<< /Type /Pages /Kids [{kids}] /Count {pageIds.Count} >>");

        writer.WriteObject(catalogId, $"<< /Type /Catalog /Pages {pagesId} 0 R >>");

        for (var i = 0; i < pages.Count; i++)
        {
            writer.WriteObject(pageIds[i],
                $"<< /Type /Page /Parent {pagesId} 0 R /MediaBox [0 0 {PageWidth.ToString(CultureInfo.InvariantCulture)} {PageHeight.ToString(CultureInfo.InvariantCulture)}] " +
                $"/Resources << /Font << /F1 {fontId} 0 R >> >> /Contents {contentIds[i]} 0 R >>");

            var content = BuildPageContent(request.Title, pages[i], i == 0);
            var contentBytes = Encoding.ASCII.GetByteCount(content);
            writer.WriteObject(contentIds[i], $"<< /Length {contentBytes} >>\nstream\n{content}\nendstream");
        }

        return writer.Build(catalogId);
    }

    private static List<List<string>> Paginate(IReadOnlyList<string> lines)
    {
        var pages = new List<List<string>>();
        for (var i = 0; i < lines.Count; i += LinesPerPage)
        {
            pages.Add(lines.Skip(i).Take(LinesPerPage).ToList());
        }

        return pages.Count == 0 ? [[]] : pages;
    }

    private static string BuildPageContent(string title, IReadOnlyList<string> lines, bool isFirstPage)
    {
        var sb = new StringBuilder();
        sb.Append("BT /F1 12 Tf ");
        var y = TopMargin;

        if (isFirstPage)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{LeftMargin} {y} Td ({Escape(title)}) Tj ");
            y -= LineHeight * 2;
            sb.Append(CultureInfo.InvariantCulture, $"0 {-(LineHeight * 2)} Td ");
        }
        else
        {
            sb.Append(CultureInfo.InvariantCulture, $"{LeftMargin} {y} Td ");
        }

        foreach (var line in lines)
        {
            sb.Append(CultureInfo.InvariantCulture, $"({Escape(line)}) Tj 0 {-LineHeight} Td ");
        }

        sb.Append("ET");
        return sb.ToString();
    }

    private static string Escape(string text) =>
        text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    /// <summary>Tracks object offsets so the trailing cross-reference table is byte-accurate.</summary>
    private sealed class PdfObjectWriter
    {
        private readonly List<string?> _objects = [];
        private readonly MemoryStream _buffer = new();

        public int Reserve()
        {
            _objects.Add(null);
            return _objects.Count; // PDF object numbers are 1-based.
        }

        public void WriteObject(int id, string body) => _objects[id - 1] = body;

        public byte[] Build(int catalogId)
        {
            var header = "%PDF-1.4\n";
            _buffer.Write(Encoding.ASCII.GetBytes(header));

            var offsets = new long[_objects.Count + 1];
            for (var i = 0; i < _objects.Count; i++)
            {
                offsets[i + 1] = _buffer.Position;
                var body = _objects[i] ?? throw new InvalidOperationException($"PDF object {i + 1} was reserved but never written.");
                _buffer.Write(Encoding.ASCII.GetBytes($"{i + 1} 0 obj\n{body}\nendobj\n"));
            }

            var xrefOffset = _buffer.Position;
            var xref = new StringBuilder();
            xref.Append(CultureInfo.InvariantCulture, $"xref\n0 {_objects.Count + 1}\n");
            xref.Append("0000000000 65535 f \n");
            for (var i = 1; i <= _objects.Count; i++)
            {
                xref.Append(CultureInfo.InvariantCulture, $"{offsets[i]:0000000000} 00000 n \n");
            }

            _buffer.Write(Encoding.ASCII.GetBytes(xref.ToString()));
            _buffer.Write(Encoding.ASCII.GetBytes(
                $"trailer\n<< /Size {_objects.Count + 1} /Root {catalogId} 0 R >>\nstartxref\n{xrefOffset}\n%%EOF"));

            return _buffer.ToArray();
        }
    }
}
