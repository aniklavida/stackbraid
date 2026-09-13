namespace StackBraid.Shared.Storage;

/// <summary>
/// Stores and retrieves an opaque blob by key. One implementation ships
/// today, <see cref="LocalFileStorage"/> (a real local disk, not a stub);
/// an S3-compatible implementation is a second class behind this same
/// interface, swapped in by configuration rather than by touching a caller.
/// </summary>
public interface IFileStorage
{
    Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
