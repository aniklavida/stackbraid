using Microsoft.Extensions.Options;

namespace StackBraid.Shared.Storage;

public sealed class LocalFileStorageOptions
{
    /// <summary>Root directory every key is stored under. Created on first use if missing.</summary>
    public string RootPath { get; set; } = "storage";
}

/// <summary>
/// Stores each blob as one file under <see cref="LocalFileStorageOptions.RootPath"/>.
/// A key is sanitized to a single path segment so it can never escape the
/// root — no caller-supplied path traverses outside the storage directory.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<LocalFileStorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
    }

    public Task<Stream?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        Stream? stream = File.Exists(path) ? File.OpenRead(path) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string key)
    {
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars().Concat(['/', '\\']).ToArray()));
        return Path.Combine(_root, safeKey);
    }
}
