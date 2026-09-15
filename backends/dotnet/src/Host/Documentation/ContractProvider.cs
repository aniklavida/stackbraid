namespace StackBraid.Host.Documentation;

/// <summary>
/// Resolves the raw bytes of the repository's contract/openapi.yaml file.
///
/// Precedence:
/// 1. Configured path via Contract:Path or STACKBRAID_CONTRACT_PATH env var.
/// 2. Local disk file if contract/openapi.yaml exists in parent directories (local development).
/// 3. Embedded resource compiled into the Host assembly at build time (deployed self-contained artifact).
/// </summary>
public static class ContractProvider
{
    private static readonly Lazy<byte[]> LazyEmbeddedBytes = new(LoadEmbeddedContractBytes);

    public static byte[] GetEmbeddedContractBytes() => LazyEmbeddedBytes.Value;

    public static byte[] GetContractBytes(IConfiguration? configuration = null)
    {
        var configuredPath = configuration?["Contract:Path"]
            ?? Environment.GetEnvironmentVariable("STACKBRAID_CONTRACT_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            if (File.Exists(configuredPath))
            {
                return File.ReadAllBytes(configuredPath);
            }

            throw new FileNotFoundException($"Configured OpenAPI contract file not found: {configuredPath}", configuredPath);
        }

        var diskPath = FindContractOnDisk();
        if (diskPath != null && File.Exists(diskPath))
        {
            return File.ReadAllBytes(diskPath);
        }

        return LazyEmbeddedBytes.Value;
    }

    private static byte[] LoadEmbeddedContractBytes()
    {
        var assembly = typeof(ContractProvider).Assembly;
        var resourceName = "StackBraid.Host.Contract.openapi.yaml";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded contract resource '{resourceName}'.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static string? FindContractOnDisk()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "contract", "openapi.yaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "contract", "openapi.yaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        return null;
    }
}
