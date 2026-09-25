using System.Buffers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AsanRezerve.Infrastructure.Core.Caching;

/// <summary>State of the second-level cache's circuit breaker, as the admin cache page shows it.</summary>
public enum CircuitState
{
    /// <summary>Redis is used normally.</summary>
    Closed,

    /// <summary>Redis is being skipped after repeated failures; every call is a miss or a no-op.</summary>
    Open,

    /// <summary>The cooldown has elapsed; the next call is a trial that closes or re-opens the circuit.</summary>
    HalfOpen,
}

/// <summary>
/// Circuit-breaker decorator for the distributed cache HybridCache uses as its second level.
/// <para>The cache is an optimization, never a dependency. When Redis is unreachable every call would block for the
/// connect timeout; this bounds that cost. After <see cref="CacheSettings.CircuitBreakerFailureThreshold"/>
/// consecutive failures Redis is skipped for <see cref="CacheSettings.CircuitBreakerResetSeconds"/>: reads are
/// misses and writes are dropped, so HybridCache serves from its in-process tier or runs the query. Then one trial
/// call is let through.</para>
/// <para>Deliberately NOT registered as the application's <see cref="IDistributedCache"/>: OTP state and
/// rate-limit counters also live there, and those must keep failing closed rather than read as "no entry".</para>
/// </summary>
public sealed class ResilientDistributedCache : IBufferDistributedCache
{
    private readonly IDistributedCache _inner;
    private readonly IBufferDistributedCache? _innerBuffer;
    private readonly CacheSettings _settings;
    private readonly ILogger<ResilientDistributedCache> _logger;
    private readonly TimeProvider _time;

    // Shared process-wide (singleton) and touched by concurrent requests, hence the interlocked access.
    private int _consecutiveFailures;
    private long _openedAtTicks;
    private string? _lastError;

    public ResilientDistributedCache(
        IDistributedCache inner,
        IOptions<CacheSettings> settings,
        ILogger<ResilientDistributedCache> logger,
        TimeProvider time)
    {
        _inner = inner;
        _innerBuffer = inner as IBufferDistributedCache;
        _settings = settings.Value;
        _logger = logger;
        _time = time;
    }

    /// <summary>Consecutive failures since the last success.</summary>
    public int ConsecutiveFailures => Volatile.Read(ref _consecutiveFailures);

    /// <summary>The last failure's message, for diagnostics; null until something fails.</summary>
    public string? LastError => Volatile.Read(ref _lastError);

    /// <summary>When the circuit last opened (UTC); null while it has never opened.</summary>
    public DateTimeOffset? OpenedAt
    {
        get
        {
            var ticks = Interlocked.Read(ref _openedAtTicks);
            return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }

    public CircuitState State
    {
        get
        {
            if (ConsecutiveFailures < Threshold)
                return CircuitState.Closed;

            return Elapsed() < Cooldown ? CircuitState.Open : CircuitState.HalfOpen;
        }
    }

    private int Threshold => Math.Max(1, _settings.CircuitBreakerFailureThreshold);

    private TimeSpan Cooldown => TimeSpan.FromSeconds(Math.Max(0, _settings.CircuitBreakerResetSeconds));

    private TimeSpan Elapsed() =>
        _time.GetUtcNow() - new DateTimeOffset(Interlocked.Read(ref _openedAtTicks), TimeSpan.Zero);

    /// <summary>True when the call must skip Redis. A half-open circuit lets the call through as the trial.</summary>
    private bool Bypass() => State == CircuitState.Open;

    private void RecordSuccess()
    {
        if (Volatile.Read(ref _consecutiveFailures) == 0) return;

        Volatile.Write(ref _consecutiveFailures, 0);
        _logger.LogInformation("Cache circuit closed: Redis is reachable again");
    }

    private void RecordFailure(Exception ex, string operation)
    {
        Volatile.Write(ref _lastError, $"{ex.GetType().Name}: {ex.Message}");
        var failures = Interlocked.Increment(ref _consecutiveFailures);

        if (failures >= Threshold)
        {
            // Opening, or a failed half-open trial re-opening: restart the cooldown either way.
            var wasOpen = failures > Threshold;
            Interlocked.Exchange(ref _openedAtTicks, _time.GetUtcNow().UtcTicks);
            if (!wasOpen)
            {
                _logger.LogError(ex,
                    "Cache circuit opened after {Failures} consecutive Redis failures; skipping Redis for {Seconds}s",
                    failures, Cooldown.TotalSeconds);
            }
            return;
        }

        _logger.LogWarning("Redis cache {Operation} failed ({Failures}/{Threshold}): {Error}",
            operation, failures, Threshold, ex.Message);
    }

    private static bool IsCallerCancellation(Exception ex, CancellationToken token) =>
        ex is OperationCanceledException && token.IsCancellationRequested;

    public byte[]? Get(string key)
    {
        if (Bypass()) return null;
        try
        {
            var value = _inner.Get(key);
            RecordSuccess();
            return value;
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "get");
            return null;
        }
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        if (Bypass()) return null;
        try
        {
            var value = await _inner.GetAsync(key, token).ConfigureAwait(false);
            RecordSuccess();
            return value;
        }
        catch (Exception ex) when (!IsCallerCancellation(ex, token))
        {
            RecordFailure(ex, "get");
            return null;
        }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        if (Bypass()) return;
        try
        {
            _inner.Set(key, value, options);
            RecordSuccess();
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "set");
        }
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        if (Bypass()) return;
        try
        {
            await _inner.SetAsync(key, value, options, token).ConfigureAwait(false);
            RecordSuccess();
        }
        catch (Exception ex) when (!IsCallerCancellation(ex, token))
        {
            RecordFailure(ex, "set");
        }
    }

    public void Refresh(string key)
    {
        if (Bypass()) return;
        try
        {
            _inner.Refresh(key);
            RecordSuccess();
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "refresh");
        }
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        if (Bypass()) return;
        try
        {
            await _inner.RefreshAsync(key, token).ConfigureAwait(false);
            RecordSuccess();
        }
        catch (Exception ex) when (!IsCallerCancellation(ex, token))
        {
            RecordFailure(ex, "refresh");
        }
    }

    public void Remove(string key)
    {
        if (Bypass()) return;
        try
        {
            _inner.Remove(key);
            RecordSuccess();
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "remove");
        }
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        if (Bypass()) return;
        try
        {
            await _inner.RemoveAsync(key, token).ConfigureAwait(false);
            RecordSuccess();
        }
        catch (Exception ex) when (!IsCallerCancellation(ex, token))
        {
            RecordFailure(ex, "remove");
        }
    }

    // IBufferDistributedCache: HybridCache prefers these (no intermediate byte[]); Redis' cache implements them.
    // When the inner cache does not, they fall back to the byte[] members above.

    public bool TryGet(string key, IBufferWriter<byte> destination)
    {
        if (_innerBuffer is null)
        {
            var value = Get(key);
            if (value is null) return false;
            destination.Write(value);
            return true;
        }

        if (Bypass()) return false;
        try
        {
            var found = _innerBuffer.TryGet(key, destination);
            RecordSuccess();
            return found;
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "get");
            return false;
        }
    }

    public async ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token = default)
    {
        if (_innerBuffer is null)
        {
            var value = await GetAsync(key, token).ConfigureAwait(false);
            if (value is null) return false;
            destination.Write(value);
            return true;
        }

        if (Bypass()) return false;
        try
        {
            var found = await _innerBuffer.TryGetAsync(key, destination, token).ConfigureAwait(false);
            RecordSuccess();
            return found;
        }
        catch (Exception ex) when (!IsCallerCancellation(ex, token))
        {
            RecordFailure(ex, "get");
            return false;
        }
    }

    public void Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options)
    {
        if (_innerBuffer is null)
        {
            Set(key, value.ToArray(), options);
            return;
        }

        if (Bypass()) return;
        try
        {
            _innerBuffer.Set(key, value, options);
            RecordSuccess();
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "set");
        }
    }

    public async ValueTask SetAsync(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        if (_innerBuffer is null)
        {
            await SetAsync(key, value.ToArray(), options, token).ConfigureAwait(false);
            return;
        }

        if (Bypass()) return;
        try
        {
            await _innerBuffer.SetAsync(key, value, options, token).ConfigureAwait(false);
            RecordSuccess();
        }
        catch (Exception ex) when (!IsCallerCancellation(ex, token))
        {
            RecordFailure(ex, "set");
        }
    }
}
