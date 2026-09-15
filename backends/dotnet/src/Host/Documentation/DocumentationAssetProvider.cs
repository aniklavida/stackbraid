using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace StackBraid.Host.Documentation;

/// <summary>
/// Serves the vendored Swagger UI browser assets from
/// contract/docs-assets/swagger-ui, compiled into this assembly at build time.
///
/// Unlike the contract itself there is no local-disk precedence here: these
/// files are a byte-identical copy of a published package, are never edited in
/// place, and the embedded copy is what a deployed artifact must serve. Reading
/// them from the assembly makes the documentation page work wherever the binary
/// runs, with no directory layout to get right and no network to reach.
/// </summary>
public static class DocumentationAssetProvider
{
    private const string ResourcePrefix = "StackBraid.Host.DocumentationAssets.";

    /// <summary>
    /// The only files served under /docs/assets/, each with the content type it
    /// must be sent as. An explicit allow-list rather than a path join: a request
    /// for a traversal sequence resolves to a name that is simply not a key here.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> MediaTypes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["swagger-ui.css"] = "text/css; charset=utf-8",
        ["swagger-ui-bundle.js"] = "application/javascript; charset=utf-8",
    };

    private static readonly ConcurrentDictionary<string, byte[]> Cache = new(StringComparer.Ordinal);

    public static IEnumerable<string> AssetFileNames => MediaTypes.Keys;

    public static bool TryGetAsset(
        string fileName,
        [NotNullWhen(true)] out byte[]? bytes,
        [NotNullWhen(true)] out string? mediaType)
    {
        if (!MediaTypes.TryGetValue(fileName, out var resolvedMediaType))
        {
            bytes = null;
            mediaType = null;
            return false;
        }

        bytes = Cache.GetOrAdd(fileName, LoadEmbeddedAssetBytes);
        mediaType = resolvedMediaType;
        return true;
    }

    private static byte[] LoadEmbeddedAssetBytes(string fileName)
    {
        var assembly = typeof(DocumentationAssetProvider).Assembly;
        var resourceName = ResourcePrefix + fileName;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Missing embedded documentation asset '{resourceName}'. It is declared in StackBraid.Host.csproj and copied from contract/docs-assets/swagger-ui.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
