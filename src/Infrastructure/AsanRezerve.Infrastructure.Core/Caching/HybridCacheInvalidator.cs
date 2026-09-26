using AsanRezerve.Core.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.Infrastructure.Core.Caching;

/// <summary>
/// <see cref="ICacheInvalidator"/> over <see cref="HybridCache"/> tags. Scoped: it remembers what the scope
/// invalidated and evicts it once more when the scope ends (see <see cref="ICacheInvalidator"/> for why).
/// </summary>
public sealed class HybridCacheInvalidator : ICacheInvalidator, IAsyncDisposable, IDisposable
{
    /// <summary>HybridCache's tag for "every entry".</summary>
    public const string AllTag = "*";

    private readonly HybridCache _cache;
    private readonly ILogger<HybridCacheInvalidator> _logger;
    private readonly HashSet<string> _pending = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public HybridCacheInvalidator(HybridCache cache, ILogger<HybridCacheInvalidator> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task InvalidateAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        var list = tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.Ordinal).ToArray();
        if (list.Length == 0) return;

        lock (_gate)
        {
            _pending.UnionWith(list);
        }

        await EvictAsync(list, cancellationToken).ConfigureAwait(false);
    }

    public Task InvalidateAllAsync(CancellationToken cancellationToken = default) =>
        InvalidateAsync([AllTag], cancellationToken);

    public async ValueTask DisposeAsync()
    {
        var tags = TakePending();
        if (tags.Length > 0)
            await EvictAsync(tags, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// A scope disposed synchronously (<c>using var scope = CreateScope()</c> in a background service) still gets
    /// its second eviction. There is no synchronization context in ASP.NET Core, so waiting here cannot deadlock.
    /// </summary>
    public void Dispose()
    {
        var tags = TakePending();
        if (tags.Length > 0)
            EvictAsync(tags, CancellationToken.None).GetAwaiter().GetResult();
    }

    private string[] TakePending()
    {
        lock (_gate)
        {
            var tags = _pending.ToArray();
            _pending.Clear();
            return tags;
        }
    }

    private async Task EvictAsync(string[] tags, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveByTagAsync(tags, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Cache invalidated for tags {Tags}", tags);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Cache invalidation failed for tags {Tags}; entries expire on their own lifetime", tags);
        }
    }
}
