using AsanRezerve.Core.Application.Abstractions.Caching;
using AsanRezerve.Infrastructure.Core.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AsanRezerve.Infrastructure.Core.UnitTests.Caching;

/// <summary>
/// Tag invalidation that survives the commit race.
/// <para>Domain events are dispatched BEFORE <c>SaveChanges</c> (EfCoreUnitOfWork), so an eviction made only then
/// can be undone: a read running between the eviction and the commit loads the old row and caches it again, and
/// the page stays stale until the entry expires. The invalidator therefore evicts once when asked and once more
/// when the DI scope — the request or the job — ends, i.e. after the unit of work committed.</para>
/// </summary>
public sealed class CacheInvalidatorTests
{
    private static ServiceProvider Build() =>
        new ServiceCollection()
            .AddLogging()
            .AddAsanRezerveCaching(new ConfigurationBuilder()
                .AddInMemoryCollection([new("Cache:Provider", "InMemory")])
                .Build())
            .BuildServiceProvider();

    private static ValueTask<string> Cache(HybridCache cache, string key, string value, params string[] tags) =>
        cache.GetOrCreateAsync(key, _ => ValueTask.FromResult(value), tags: tags);

    private static async Task<bool> IsCached(HybridCache cache, string key)
    {
        var missed = false;
        await cache.GetOrCreateAsync(key, _ =>
        {
            missed = true;
            return ValueTask.FromResult("reloaded");
        });
        return !missed;
    }

    [Fact]
    public async Task Invalidate_EvictsEveryEntryCarryingTheTag_AndNothingElse()
    {
        await using var sp = Build();
        var cache = sp.GetRequiredService<HybridCache>();
        await Cache(cache, "salon-page:1", "old", "provider:1");
        await Cache(cache, "salon-list", "old", "provider:1", "provider-directory");
        await Cache(cache, "salon-page:2", "other", "provider:2");

        await using (var scope = sp.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ICacheInvalidator>().InvalidateAsync(["provider:1"]);
        }

        (await IsCached(cache, "salon-page:1")).Should().BeFalse();
        (await IsCached(cache, "salon-list")).Should().BeFalse();
        (await IsCached(cache, "salon-page:2")).Should().BeTrue("another salon's page was not changed");
    }

    [Fact]
    public async Task AReadThatRecachedTheOldRowBeforeCommit_IsEvictedAgain_WhenTheScopeEnds()
    {
        await using var sp = Build();
        var cache = sp.GetRequiredService<HybridCache>();
        await Cache(cache, "salon-page:1", "old", "provider:1");

        await using (var scope = sp.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ICacheInvalidator>().InvalidateAsync(["provider:1"]);

            // A concurrent request reads the not-yet-committed-over row and caches it again.
            await Cache(cache, "salon-page:1", "old", "provider:1");
            (await IsCached(cache, "salon-page:1")).Should().BeTrue();
        } // the unit of work committed; the request scope ends

        (await IsCached(cache, "salon-page:1")).Should().BeFalse();
    }

    [Fact]
    public async Task ASynchronouslyDisposedScope_AlsoEvictsAgain()
    {
        // Background services often write `using var scope = CreateScope()`; an IAsyncDisposable-only service would
        // make that throw, and a skipped second eviction would leave the race open.
        await using var sp = Build();
        var cache = sp.GetRequiredService<HybridCache>();
        await Cache(cache, "salon-page:1", "old", "provider:1");

        using (var scope = sp.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ICacheInvalidator>().InvalidateAsync(["provider:1"]);
            await Cache(cache, "salon-page:1", "old", "provider:1");
        }

        (await IsCached(cache, "salon-page:1")).Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateAll_EvictsEverything()
    {
        await using var sp = Build();
        var cache = sp.GetRequiredService<HybridCache>();
        await Cache(cache, "a", "1", "x");
        await Cache(cache, "b", "2");

        await using (var scope = sp.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ICacheInvalidator>().InvalidateAllAsync();
        }

        (await IsCached(cache, "a")).Should().BeFalse();
        (await IsCached(cache, "b")).Should().BeFalse();
    }

    [Fact]
    public async Task AScopeThatInvalidatedNothing_TouchesNothingWhenItEnds()
    {
        var cache = Substitute.For<HybridCache>();
        var sut = new HybridCacheInvalidator(cache, NullLogger<HybridCacheInvalidator>.Instance);

        await sut.DisposeAsync();

        cache.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ACacheFailure_NeverFailsTheCommandThatChangedTheData()
    {
        var cache = Substitute.For<HybridCache>();
        cache.RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException(new InvalidOperationException("boom")));
        cache.RemoveByTagAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException(new InvalidOperationException("boom")));
        var sut = new HybridCacheInvalidator(cache, NullLogger<HybridCacheInvalidator>.Instance);

        var invalidate = () => sut.InvalidateAsync(["provider:1"]);
        var end = async () => await sut.DisposeAsync();

        await invalidate.Should().NotThrowAsync("a stale page for one lifetime is better than a failed save");
        await end.Should().NotThrowAsync();
    }

    [Fact]
    public void Metrics_CountHitsAndMissesPerRegion()
    {
        var metrics = new CacheMetrics();

        metrics.Record("GetProviderByIdQuery", hit: false);
        metrics.Record("GetProviderByIdQuery", hit: true);
        metrics.Record("GetProviderByIdQuery", hit: true);
        metrics.Record("GetProviderByIdQuery", hit: true);
        metrics.Record("SearchProvidersQuery", hit: false);

        var snapshot = metrics.Snapshot();

        snapshot.Select(r => r.Region).Should().Equal("GetProviderByIdQuery", "SearchProvidersQuery");
        var salon = snapshot[0];
        salon.Requests.Should().Be(4);
        salon.Hits.Should().Be(3);
        salon.Misses.Should().Be(1);
        salon.HitRatio.Should().Be(0.75);
        snapshot[1].HitRatio.Should().Be(0);
    }
}
