using System.Globalization;

namespace StackBraid.Shared.Localization;

/// <summary>
/// Parses an <c>Accept-Language</c> header value into the single supported
/// culture that best matches it. This is the one negotiation rule this
/// backend shares with the Python backend's
/// <c>app.shared.localization.localizer.JsonAppLocalizer._normalize</c> —
/// same parsing steps, same precedence, so the two backends resolve an
/// identical header to an identical culture.
/// </summary>
public static class AcceptLanguageNegotiator
{
    /// <summary>
    /// <paramref name="header"/> is a comma-separated list of language
    /// ranges, each optionally carrying a <c>;q=</c> weight (RFC 9110 §12.5.4).
    /// A range with no weight defaults to 1.0, and a weight that fails to
    /// parse is treated the same way rather than dropping the range —
    /// a slightly malformed header should still degrade to "pick something
    /// reasonable", not "ignore the whole header". Ranges are ordered by
    /// weight, highest first, keeping the header's own order for ties
    /// (a stable sort). Each range's primary subtag ("es-MX" -&gt;
    /// "es") is matched against <paramref name="supportedCultures"/>; the
    /// first match wins. No match anywhere returns
    /// <paramref name="defaultCulture"/>.
    /// </summary>
    public static string Negotiate(string? header, IReadOnlyList<string> supportedCultures, string defaultCulture)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return defaultCulture;
        }

        var ranges = header
            .Split(',')
            .Select((raw, index) => (Range: ParseRange(raw), Index: index))
            .Where(x => x.Range is not null)
            .OrderByDescending(x => x.Range!.Value.Weight)
            .ThenBy(x => x.Index);

        foreach (var (range, _) in ranges)
        {
            var primary = range!.Value.PrimarySubtag;
            if (supportedCultures.Contains(primary))
            {
                return primary;
            }
        }

        return defaultCulture;
    }

    private static (string PrimarySubtag, double Weight)? ParseRange(string raw)
    {
        var parts = raw.Split(';');
        var tag = parts[0].Trim();
        if (tag.Length == 0)
        {
            return null;
        }

        var weight = 1.0;
        for (var i = 1; i < parts.Length; i++)
        {
            var param = parts[i].Trim();
            if (!param.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (double.TryParse(param.AsSpan(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                weight = parsed;
            }

            break;
        }

        var primary = tag.Split('-')[0].ToLowerInvariant();
        return (primary, weight);
    }
}
