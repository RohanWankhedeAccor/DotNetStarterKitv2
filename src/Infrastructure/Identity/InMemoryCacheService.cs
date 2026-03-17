using System.Collections.Concurrent;
using Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Identity;

/// <summary>
/// In-memory implementation of <see cref="ICacheService"/> backed by ASP.NET Core's
/// <see cref="IMemoryCache"/>. Registered as a singleton so the cache is shared across
/// all requests within one process lifetime.
///
/// <para>
/// Supports prefix-based eviction via an internal <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// that mirrors the set of active keys — <see cref="IMemoryCache"/> itself does not expose keys.
/// Post-eviction callbacks keep the key dictionary in sync with the cache on TTL expiry.
/// </para>
///
/// <para>
/// To swap out for a distributed cache (Redis, SQL), create a new class that implements
/// <see cref="ICacheService"/> and update the DI registration in
/// <c>InfrastructureServiceExtensions</c>. Application-layer handlers need no changes.
/// </para>
/// </summary>
public sealed class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Tracks all currently stored keys so <see cref="RemoveByPrefix"/> can enumerate them.
    /// Updated by post-eviction callbacks to stay in sync when TTL expires naturally.
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> _keys = new();

    /// <summary>Default TTL when no <c>absoluteExpiration</c> is supplied by the caller.</summary>
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes the cache service with the given <see cref="IMemoryCache"/> instance.
    /// </summary>
    public InMemoryCacheService(IMemoryCache cache) => _cache = cache;

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? cached))
            return cached!;

        var value = await factory(cancellationToken);

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration ?? DefaultExpiration,
        };

        // Keep _keys accurate on natural eviction (TTL / memory pressure)
        options.RegisterPostEvictionCallback(
            (evictedKey, _, _, _) => _keys.TryRemove((string)evictedKey, out _));

        _cache.Set(key, value, options);
        _keys.TryAdd(key, true);

        return value;
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public void RemoveByPrefix(string keyPrefix)
    {
        foreach (var key in _keys.Keys.Where(k => k.StartsWith(keyPrefix, StringComparison.Ordinal)))
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }
    }
}
