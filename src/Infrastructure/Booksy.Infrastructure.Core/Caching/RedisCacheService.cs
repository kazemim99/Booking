// ========================================
// Caching/RedisCacheService.cs
// ========================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using IDatabase = StackExchange.Redis.IDatabase;

namespace Booksy.Infrastructure.Core.Caching;

/// <summary>
/// Redis implementation of caching service
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly CacheSettings _settings;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Circuit-breaker state. Instance fields are shared process-wide because
    // this service is registered as a singleton — the breaker would be
    // useless if each request started closed and paid the connect timeout
    // again. Accessed from concurrent requests, hence the interlocked reads.
    private int _consecutiveFailures;
    private long _circuitOpenedAtTicks;

    /// <summary>Test seam: current consecutive-failure count.</summary>
    internal int ConsecutiveFailures => Volatile.Read(ref _consecutiveFailures);

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IOptions<CacheSettings> settings,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _settings = settings.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// True while Redis is considered down. Callers skip the operation
    /// entirely rather than blocking for the connect timeout — a cache miss
    /// costs a database read; a stalled request costs the whole page.
    /// </summary>
    private bool IsCircuitOpen
    {
        get
        {
            if (Volatile.Read(ref _consecutiveFailures) < _settings.CircuitBreakerFailureThreshold)
                return false;

            var openedAt = Interlocked.Read(ref _circuitOpenedAtTicks);
            var elapsed = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - openedAt);

            if (elapsed < TimeSpan.FromSeconds(_settings.CircuitBreakerResetSeconds))
                return true;

            // Cooldown elapsed: let one trial request through (half-open).
            Volatile.Write(ref _consecutiveFailures, _settings.CircuitBreakerFailureThreshold - 1);
            return false;
        }
    }

    private void RecordSuccess()
    {
        if (Volatile.Read(ref _consecutiveFailures) == 0) return;

        Volatile.Write(ref _consecutiveFailures, 0);
        _logger.LogInformation("Cache circuit closed — Redis is reachable again");
    }

    private void RecordFailure(Exception ex, string operation, string key)
    {
        var failures = Interlocked.Increment(ref _consecutiveFailures);

        if (failures == _settings.CircuitBreakerFailureThreshold)
        {
            Interlocked.Exchange(ref _circuitOpenedAtTicks, DateTime.UtcNow.Ticks);
            _logger.LogError(ex,
                "Cache circuit opened after {Failures} consecutive failures; " +
                "bypassing Redis for {Seconds}s and reading through to the source",
                failures, _settings.CircuitBreakerResetSeconds);
        }
        else if (failures < _settings.CircuitBreakerFailureThreshold)
        {
            _logger.LogError(ex, "Error during cache {Operation} for key {Key}", operation, key);
        }
        // Past the threshold the circuit is already open and logged: stay quiet
        // rather than filling the log with one stack trace per request.
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (IsCircuitOpen) return null;

        try
        {
            var value = await _database.StringGetAsync(GetKey(key));
            RecordSuccess();

            if (!value.HasValue)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "get", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (IsCircuitOpen) return;

        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            var exp = expiration ?? TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes);

            await _database.StringSetAsync(GetKey(key), serialized, exp);
            RecordSuccess();

            _logger.LogDebug("Cached value for key {Key} with expiration {Expiration}", key, exp);
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "set", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen) return;

        try
        {
            await _database.KeyDeleteAsync(GetKey(key));
            RecordSuccess();
            _logger.LogDebug("Removed cache key {Key}", key);
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "remove", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen) return;

        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: GetKey(pattern)).ToArray();

            if (keys.Any())
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} keys matching pattern {Pattern}", keys.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "remove-by-pattern", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen) return false;

        try
        {
            var exists = await _database.KeyExistsAsync(GetKey(key));
            RecordSuccess();
            return exists;
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "exists", key);
            return false;
        }
    }

    public async Task<T> GetOrAddAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);

        if (cached != null)
        {
            return cached;
        }

        var value = await factory();
        await SetAsync(key, value, expiration, cancellationToken);

        return value;
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen) return;

        try
        {
            await _database.KeyExpireAsync(
                GetKey(key),
                TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes));
            RecordSuccess();

            _logger.LogDebug("Refreshed expiration for key {Key}", key);
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "refresh", key);
        }
    }

    private string GetKey(string key) => $"{_settings.KeyPrefix}:{key}";
}


