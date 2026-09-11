using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Booksy.Tests.Commons;

/// <summary>
/// The test host's <see cref="IDistributedCache"/>: an in-process store that a test can empty
/// between tests. It holds two things a shared host would otherwise leak across tests —
/// <c>CachingBehavior</c>'s cached query results (user/customer details, favourites, the
/// availability calendar, <c>search_users_*</c>) and AspNetCoreRateLimit's per-client counters.
///
/// <para><see cref="MemoryDistributedCache"/>, the production in-memory implementation this
/// replaces, offers only Get/Set/Refresh/Remove — there is no way to enumerate or clear its
/// entries. Rather than reimplement a store, this wraps one and swaps in a fresh instance on
/// <see cref="Reset"/>, exactly as though the process had just started.</para>
/// </summary>
public sealed class ResettableDistributedCache : IDistributedCache
{
    private volatile MemoryDistributedCache _inner = NewInner();

    public void Reset() => _inner = NewInner();

    private static MemoryDistributedCache NewInner() =>
        new(Options.Create(new MemoryDistributedCacheOptions()));

    public byte[]? Get(string key) => _inner.Get(key);

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        _inner.GetAsync(key, token);

    public void Refresh(string key) => _inner.Refresh(key);

    public Task RefreshAsync(string key, CancellationToken token = default) =>
        _inner.RefreshAsync(key, token);

    public void Remove(string key) => _inner.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default) =>
        _inner.RemoveAsync(key, token);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        _inner.Set(key, value, options);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        _inner.SetAsync(key, value, options, token);
}
