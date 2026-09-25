using AsanRezerve.Core.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AsanRezerve.Infrastructure.Core.Caching;

/// <summary>
/// The host's caching: one Redis connection, one <see cref="IDistributedCache"/>, and <see cref="HybridCache"/>
/// (in-process L1 + Redis L2, stampede protection, tag invalidation) for cached reads.
/// </summary>
public static class CachingRegistration
{
    /// <summary>
    /// DI key of the distributed cache HybridCache uses as its second level: the circuit breaker over the
    /// application's <see cref="IDistributedCache"/>.
    /// </summary>
    public const string HybridSecondLevelKey = "asanrezerve:hybrid-l2";

    /// <summary>
    /// The Redis the cache talks to: <c>Cache:RedisConnectionString</c> when set, otherwise
    /// <c>ConnectionStrings:Redis</c>; none when <c>Cache:Provider</c> is <c>InMemory</c> or nothing is configured.
    /// </summary>
    public static string? ResolveRedisConnectionString(IConfiguration configuration)
    {
        var section = configuration.GetSection("Cache");

        if (string.Equals(section["Provider"], "InMemory", StringComparison.OrdinalIgnoreCase))
            return null;

        var dedicated = section["RedisConnectionString"];
        if (!string.IsNullOrWhiteSpace(dedicated))
            return dedicated;

        var shared = configuration.GetConnectionString("Redis");
        return string.IsNullOrWhiteSpace(shared) ? null : shared;
    }

    public static IServiceCollection AddAsanRezerveCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Cache");
        services.Configure<CacheSettings>(section);
        var settings = section.Get<CacheSettings>() ?? new CacheSettings();

        // L1. Statistics feed the admin cache page (entry count, hits, misses).
        services.AddMemoryCache(options => options.TrackStatistics = true);
        services.TryAddSingleton(TimeProvider.System);

        var redis = ResolveRedisConnectionString(configuration);
        if (redis is not null)
        {
            services.TryAddSingleton(new RedisConnection(redis));
            services.AddStackExchangeRedisCache(options => options.InstanceName = $"{settings.KeyPrefix}:");
            services.AddOptions<RedisCacheOptions>()
                .Configure<RedisConnection>((options, connection) => options.ConnectionMultiplexerFactory = connection.GetAsync);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        // L2 for HybridCache: the breaker over whatever IDistributedCache DI holds when it is first resolved, so a
        // host that swaps the distributed cache (the integration tests do) is honoured.
        services.TryAddSingleton(sp => new ResilientDistributedCache(
            sp.GetRequiredService<IDistributedCache>(),
            sp.GetRequiredService<IOptions<CacheSettings>>(),
            sp.GetRequiredService<ILogger<ResilientDistributedCache>>(),
            sp.GetRequiredService<TimeProvider>()));
        services.TryAddKeyedSingleton<IDistributedCache>(
            HybridSecondLevelKey, (sp, _) => sp.GetRequiredService<ResilientDistributedCache>());

        services.AddHybridCache(options =>
        {
            options.DistributedCacheServiceKey = HybridSecondLevelKey;
            options.MaximumPayloadBytes = settings.MaximumPayloadBytes;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(Math.Max(1, settings.DefaultExpirationMinutes)),
                LocalCacheExpiration = TimeSpan.FromSeconds(Math.Max(1, settings.LocalExpirationSeconds)),
            };
        });

        services.TryAddSingleton<CacheMetrics>();
        services.TryAddSingleton<ICacheMetrics>(sp => sp.GetRequiredService<CacheMetrics>());
        services.TryAddScoped<ICacheInvalidator, HybridCacheInvalidator>();

        return services;
    }
}
