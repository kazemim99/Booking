using AsanRezerve.Infrastructure.Core.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AsanRezerve.Infrastructure.Core.UnitTests.Caching;

/// <summary>
/// Which Redis the cache talks to, and what <see cref="CachingRegistration.AddAsanRezerveCaching"/> puts in DI.
/// <para>Production compose sets only <c>ConnectionStrings__Redis</c>, while the cache used to read
/// <c>Cache:RedisConnectionString</c>, which appsettings.json pointed at localhost — inside the API container, a
/// Redis that does not exist. The cache therefore never reached Redis in production.</para>
/// </summary>
public sealed class CachingRegistrationTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static ServiceProvider Build(IConfiguration config, Action<IServiceCollection>? after = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAsanRezerveCaching(config);
        after?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void ConnectionStringsRedis_IsUsed_WhenNoCacheSpecificOneIsSet()
    {
        var config = Config(("Cache:Provider", "Redis"), ("ConnectionStrings:Redis", "redis:6379,password=x"));

        CachingRegistration.ResolveRedisConnectionString(config).Should().Be("redis:6379,password=x");
    }

    [Fact]
    public void AnExplicitCacheConnectionString_Wins()
    {
        var config = Config(
            ("Cache:Provider", "Redis"),
            ("Cache:RedisConnectionString", "cache-redis:6379"),
            ("ConnectionStrings:Redis", "redis:6379"));

        CachingRegistration.ResolveRedisConnectionString(config).Should().Be("cache-redis:6379");
    }

    [Fact]
    public void ProviderDefaultsToRedis_WhenAConnectionStringExists()
    {
        var config = Config(("ConnectionStrings:Redis", "redis:6379"));

        CachingRegistration.ResolveRedisConnectionString(config).Should().Be("redis:6379");
    }

    [Theory]
    [InlineData("InMemory")]
    [InlineData("inmemory")]
    public void InMemoryProvider_IgnoresRedis(string provider)
    {
        var config = Config(("Cache:Provider", provider), ("ConnectionStrings:Redis", "redis:6379"));

        CachingRegistration.ResolveRedisConnectionString(config).Should().BeNull();
    }

    [Fact]
    public void NoRedisConfigured_MeansNoRedis()
    {
        CachingRegistration.ResolveRedisConnectionString(Config()).Should().BeNull();
    }

    [Fact]
    public void WithoutRedis_TheDistributedCacheIsInProcess()
    {
        using var sp = Build(Config(("Cache:Provider", "InMemory")));

        sp.GetRequiredService<IDistributedCache>().Should().BeOfType<MemoryDistributedCache>();
        sp.GetRequiredService<HybridCache>().Should().NotBeNull();
    }

    [Fact]
    public void WithRedis_TheDistributedCacheIsRedis_UnderOnePrefix_AndDoesNotConnectAtRegistration()
    {
        // An address nothing listens on: building and resolving must not block on it.
        using var sp = Build(Config(("ConnectionStrings:Redis", "127.0.0.1:1,connectTimeout=100")));

        sp.GetRequiredService<IDistributedCache>().Should().BeAssignableTo<RedisCache>();
        sp.GetRequiredService<IOptions<RedisCacheOptions>>().Value.InstanceName.Should().Be("asanrezerve:");
    }

    [Fact]
    public void HybridCacheSecondLevel_IsTheBreaker_OverWhateverDistributedCacheDiHolds()
    {
        // A test host (or anything else) that swaps IDistributedCache after registration must be honoured:
        // the breaker wraps the replacement, not the original.
        var replacement = Substitute.For<IDistributedCache>();
        using var sp = Build(Config(("Cache:Provider", "InMemory")), services =>
        {
            services.AddSingleton(replacement);
        });

        var l2 = sp.GetRequiredKeyedService<IDistributedCache>(CachingRegistration.HybridSecondLevelKey);

        l2.Should().BeOfType<ResilientDistributedCache>();
        l2.Get("probe");
        replacement.Received(1).Get("probe");
        sp.GetRequiredService<ResilientDistributedCache>().Should().BeSameAs(l2);
    }

    [Fact]
    public async Task HybridCache_RunsTheFactoryOnce_ForRepeatedReads()
    {
        using var sp = Build(Config(("Cache:Provider", "InMemory")));
        var cache = sp.GetRequiredService<HybridCache>();
        var calls = 0;

        for (var i = 0; i < 3; i++)
        {
            var value = await cache.GetOrCreateAsync("k", _ =>
            {
                calls++;
                return ValueTask.FromResult("v");
            });
            value.Should().Be("v");
        }

        calls.Should().Be(1);
    }

    [Fact]
    public async Task HybridCache_StillReturnsTheFreshValue_WhenRedisIsDown()
    {
        var broken = Substitute.For<IDistributedCache>();
        broken.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("down"));
        broken.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("down"));
        using var sp = Build(Config(("Cache:Provider", "InMemory")), services => services.AddSingleton(broken));
        var cache = sp.GetRequiredService<HybridCache>();

        var value = await cache.GetOrCreateAsync("k", _ => ValueTask.FromResult("from-source"));

        // The whole point: a dead cache must not break the read path.
        value.Should().Be("from-source");
    }

    [Fact]
    public void L1_TracksStatistics_ForTheAdminCachePage()
    {
        using var sp = Build(Config(("Cache:Provider", "InMemory")));

        var memory = (MemoryCache)sp.GetRequiredService<IMemoryCache>();

        memory.GetCurrentStatistics().Should().NotBeNull("statistics are only collected when TrackStatistics is on");
    }

    [Fact]
    public void ProviderSettings_BindFromTheCacheSection()
    {
        using var sp = Build(Config(("Cache:Provider", "InMemory"), ("Cache:DefaultExpirationMinutes", "7")));

        sp.GetRequiredService<IOptions<CacheSettings>>().Value.DefaultExpirationMinutes.Should().Be(7);
        sp.GetRequiredService<ILoggerFactory>().Should().NotBeNull();
    }
}
