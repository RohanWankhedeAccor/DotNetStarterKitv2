using FluentAssertions;
using Infrastructure.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace Unit.Caching;

/// <summary>
/// Unit tests for <see cref="InMemoryCacheService"/>.
/// Verifies the get-or-set contract, manual eviction, and prefix-based eviction.
/// </summary>
public sealed class InMemoryCacheServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly InMemoryCacheService _sut;

    public InMemoryCacheServiceTests()
    {
        _sut = new InMemoryCacheService(_memoryCache);
    }

    [Fact]
    public async Task GetOrSetAsync_OnFirstCall_InvokesFactory()
    {
        var factoryCalled = 0;

        await _sut.GetOrSetAsync("key1", _ => { factoryCalled++; return Task.FromResult(42); });

        factoryCalled.Should().Be(1);
    }

    [Fact]
    public async Task GetOrSetAsync_OnSecondCall_ReturnsCachedValueWithoutCallingFactory()
    {
        var factoryCalled = 0;

        await _sut.GetOrSetAsync("key2", _ => { factoryCalled++; return Task.FromResult("hello"); });
        var result = await _sut.GetOrSetAsync("key2", _ => { factoryCalled++; return Task.FromResult("world"); });

        factoryCalled.Should().Be(1, "factory is only called on a cache miss");
        result.Should().Be("hello", "the cached value from the first call is returned");
    }

    [Fact]
    public async Task Remove_EvictsEntry_FactoryCalledAgainAfterRemoval()
    {
        var calls = 0;

        await _sut.GetOrSetAsync("key3", _ => { calls++; return Task.FromResult(1); });
        _sut.Remove("key3");
        await _sut.GetOrSetAsync("key3", _ => { calls++; return Task.FromResult(2); });

        calls.Should().Be(2, "factory must be invoked again after the entry is removed");
    }

    [Fact]
    public async Task RemoveByPrefix_EvictsAllMatchingEntries()
    {
        var values = new Dictionary<string, int>();

        await _sut.GetOrSetAsync("users:p1", _ => Task.FromResult(1));
        await _sut.GetOrSetAsync("users:p2", _ => Task.FromResult(2));
        await _sut.GetOrSetAsync("roles:list", _ => Task.FromResult(3));

        _sut.RemoveByPrefix("users:");

        // Users cache entries should be gone — factories called again.
        int u1 = 0, u2 = 0, roles = 0;
        await _sut.GetOrSetAsync("users:p1", _ => { u1++; return Task.FromResult(10); });
        await _sut.GetOrSetAsync("users:p2", _ => { u2++; return Task.FromResult(20); });
        await _sut.GetOrSetAsync("roles:list", _ => { roles++; return Task.FromResult(30); });

        u1.Should().Be(1, "users:p1 was evicted by prefix removal");
        u2.Should().Be(1, "users:p2 was evicted by prefix removal");
        roles.Should().Be(0, "roles:list was NOT evicted — different prefix");
    }

    [Fact]
    public async Task RemoveByPrefix_WithNonMatchingPrefix_LeavesEntriesIntact()
    {
        var factoryCalled = 0;

        await _sut.GetOrSetAsync("users:p1", _ => Task.FromResult("cached"));
        _sut.RemoveByPrefix("roles:");  // Different prefix — should not affect users entries

        await _sut.GetOrSetAsync("users:p1", _ => { factoryCalled++; return Task.FromResult("new"); });

        factoryCalled.Should().Be(0, "entry was not evicted by an unrelated prefix");
    }

    public void Dispose() => _memoryCache.Dispose();
}
