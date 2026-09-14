using System.Collections.Concurrent;
using System.Text.Json;

namespace StackBraid.Shared.Localization;

/// <summary>
/// Loads one flat key→text JSON file per supported culture, embedded in this
/// assembly under <c>Localization/Resources</c>. No templating engine, no
/// resx/satellite-assembly toolchain — <c>{placeholder}</c> substitution is
/// the one feature the error messages this ships actually need.
/// </summary>
public sealed class JsonAppLocalizer : IAppLocalizer
{
    public const string DefaultCulture = "en";

    private static readonly string[] EmbeddedCultures = ["en", "es"];

    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _cache = new();

    public IReadOnlyList<string> SupportedCultures { get; } = EmbeddedCultures;

    public string GetString(string key, string? culture = null, IReadOnlyDictionary<string, string>? arguments = null)
    {
        var resolvedCulture = NormalizeCulture(culture);
        var text = Load(resolvedCulture).GetValueOrDefault(key)
                   ?? Load(DefaultCulture).GetValueOrDefault(key)
                   ?? key;

        if (arguments is null || arguments.Count == 0)
        {
            return text;
        }

        foreach (var (name, value) in arguments)
        {
            text = text.Replace("{" + name + "}", value, StringComparison.Ordinal);
        }

        return text;
    }

    private string NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return DefaultCulture;
        }

        // The one locale-negotiation rule shared with the Python backend
        // (see app/shared/localization/localizer.py's `_normalize`):
        // an `Accept-Language` header is a comma-separated list of language
        // ranges, each optionally carrying a `;q=` weight (default 1.0 when
        // absent or unparsable). Highest weight wins, ties keep header
        // order, and each range's primary subtag ("es-MX" -> "es") is what
        // is actually matched against what this catalogue ships — this
        // catalogue does not distinguish regional variants of a language.
        // The previous version only ever looked at the text before the
        // first hyphen in the *whole* header, so a single-tag request like
        // "es-MX" worked by accident but a real multi-value header such as
        // "fr,es;q=0.8,en;q=0.6" fell straight through to the default
        // culture instead of finding "es".
        return AcceptLanguageNegotiator.Negotiate(culture, SupportedCultures, DefaultCulture);
    }

    private IReadOnlyDictionary<string, string> Load(string culture) =>
        _cache.GetOrAdd(culture, static c =>
        {
            var assembly = typeof(JsonAppLocalizer).Assembly;
            var resourceName = $"StackBraid.Shared.Localization.Resources.{c}.json";
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Missing embedded localization resource '{resourceName}'.");
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
                ?? throw new InvalidOperationException($"Localization resource '{resourceName}' did not deserialize to a string map.");
            return map;
        });
}
