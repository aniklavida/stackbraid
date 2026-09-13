using Microsoft.Extensions.Caching.Memory;

namespace StackBraid.Shared.Caching;

public sealed class InMemoryCache : ICache
{
    private readonly IMemoryCache _cache;

    public InMemoryCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return await factory(cancellationToken).ConfigureAwait(false);
        })!;

    public void Remove(string key) => _cache.Remove(key);
}
