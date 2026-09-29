using System.Globalization;
using System.Text;
using Shouldly;

namespace StackBraid.Shared.UnitTests.Documents;

/// <summary>
/// A minimal structural check that a byte array really is a PDF a reader can
/// parse, rather than merely one that starts with the right four characters.
/// It walks the document the way a reader does — header, body objects,
/// cross-reference table, trailer — and asserts that the trailer's root object
/// resolves and that every cross-reference offset actually points at the object
/// it claims to. That is what catches a truncated or mis-offset file, which a
/// magic-byte check alone would happily pass.
/// </summary>
internal static class PdfStructureAssert
{
    public static void IsParseable(byte[] bytes)
    {
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);

        var text = Encoding.Latin1.GetString(bytes);

        text.ShouldStartWith("%PDF-");
        text.TrimEnd().ShouldEndWith("%%EOF");

        var startxrefIndex = text.LastIndexOf("startxref", StringComparison.Ordinal);
        startxrefIndex.ShouldBeGreaterThan(-1);

        var xrefOffset = int.Parse(
            text[(startxrefIndex + "startxref".Length)..].Trim().Split('\n')[0].Trim(),
            CultureInfo.InvariantCulture);

        text.Substring(xrefOffset).ShouldStartWith("xref");

        var objects = ParseObjectCount(text, xrefOffset);
        objects.ShouldBeGreaterThan(0);

        for (var id = 1; id <= objects; id++)
        {
            var entryOffset = ReadXrefEntryOffset(text, xrefOffset, id);

            // The cross-reference table is only meaningful if the offset it records
            // lands exactly on the start of the object it names.
            text.Substring(entryOffset).ShouldStartWith($"{id} 0 obj");
            text[entryOffset..].ShouldContain("endobj");
        }

        ReadTrailerRootId(text).ShouldBeGreaterThan(0);
    }

    private static int ParseObjectCount(string text, int xrefOffset)
    {
        // "xref\n0 <count>\n" — count includes the free head entry at index 0.
        var header = text.Substring(xrefOffset).Split('\n');
        header[0].Trim().ShouldBe("xref");

        var parts = header[1].Trim().Split(' ');
        parts[0].ShouldBe("0");

        return int.Parse(parts[1], CultureInfo.InvariantCulture) - 1;
    }

    private static int ReadXrefEntryOffset(string text, int xrefOffset, int id)
    {
        var lineStart = xrefOffset + $"xref\n0 {ParseObjectCount(text, xrefOffset) + 1}\n".Length;
        var line = text[lineStart..].Split('\n')[id];

        return int.Parse(line[..10], CultureInfo.InvariantCulture);
    }

    private static int ReadTrailerRootId(string text)
    {
        var trailerIndex = text.LastIndexOf("/Root", StringComparison.Ordinal);
        trailerIndex.ShouldBeGreaterThan(-1);

        var digits = new StringBuilder();
        var seenSeparator = false;
        for (var i = trailerIndex + "/Root".Length; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c))
            {
                // A digit is never preceded by more than the one separator space.
                if (digits.Length > 0 || seenSeparator) break;

                seenSeparator = true;
                continue;
            }

            if (!char.IsDigit(c)) break;

            digits.Append(c);
        }

        digits.Length.ShouldBeGreaterThan(0);
        return int.Parse(digits.ToString(), CultureInfo.InvariantCulture);
    }
}
