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

        // "es-ES" falls back to the "es" bucket — this catalogue does not
        // distinguish regional variants of a language.
        var primary = culture.Split('-')[0].ToLowerInvariant();
        return SupportedCultures.Contains(primary) ? primary : DefaultCulture;
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
