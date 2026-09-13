namespace StackBraid.Shared.Caching;

/// <summary>
/// Reads through a cache, computing and storing the value on a miss. One
/// implementation ships today, <see cref="InMemoryCache"/> — correct for a
/// single instance, and exactly what a distributed cache (Redis, per
/// <c>docs/SPEC.md</c>'s dependency table) is a second implementation of
/// once the deployment is more than one instance.
/// </summary>
public interface ICache
{
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken = default);

    void Remove(string key);
}
