namespace Application.Interfaces;

/// <summary>
/// Abstraction for application-level caching.
///
/// <para>
/// Handlers depend on this interface — never on <c>IMemoryCache</c> or any specific
/// cache technology — keeping the Application layer free of Infrastructure concerns.
/// The Infrastructure layer provides the concrete implementation (in-memory today,
/// Redis or distributed cache in future environments).
/// </para>
///
/// <para><b>Key conventions</b></para>
/// Use structured, hierarchical keys to avoid collisions across features:
/// <c>"users:page:1:10"</c>, <c>"users:id:abc123"</c>, <c>"roles:list"</c>.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Returns the cached value for <paramref name="key"/> if present; otherwise invokes
    /// <paramref name="factory"/>, stores the result under <paramref name="key"/>, and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">A unique cache key for this value.</param>
    /// <param name="factory">
    /// An async delegate called only on a cache miss. Must not return <c>null</c>
    /// for non-nullable types — cache stores and returns whatever the factory produces.
    /// </param>
    /// <param name="absoluteExpiration">
    /// How long the entry lives from the time it is inserted.
    /// <c>null</c> uses the implementation's default (typically 5 minutes).
    /// </param>
    /// <param name="cancellationToken">Cancellation token forwarded to <paramref name="factory"/>.</param>
    /// <returns>The cached or freshly computed value.</returns>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the entry identified by <paramref name="key"/> from the cache.
    /// No-op when the key is not present.
    /// </summary>
    /// <param name="key">The cache key to evict.</param>
    void Remove(string key);

    /// <summary>
    /// Removes all entries whose keys begin with <paramref name="keyPrefix"/>.
    /// Use this to invalidate a group of related entries (e.g., all user pages).
    /// </summary>
    /// <param name="keyPrefix">The prefix shared by all entries to evict.</param>
    void RemoveByPrefix(string keyPrefix);
}
