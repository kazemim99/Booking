using AsanRezerve.Core.Application.Abstractions.Caching;
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Behaviors;
using AsanRezerve.Infrastructure.Core.Caching;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Behaviors;

/// <summary>
/// The query cache: a query opts in (<see cref="IQuery{TResponse}.IsCacheable"/>) and is then served from
/// HybridCache — absolute lifetime, tags for invalidation, one handler run per key under concurrent misses.
/// <para>It replaced a sliding-expiration cache with no invalidation, under which a read that kept being made was
/// never refreshed at all (the availability calendar, customer details, favourites), and whose hand-written keys
/// could leave filters out.</para>
/// </summary>
public sealed class CachingBehaviorTests : IAsyncDisposable
{
    public sealed record Salon(Guid Id, string Name);

    public sealed record GetSalon(Guid Id, string? Filter = null) : IQuery<Salon?>
    {
        public bool IsCacheable => true;
        public int? CacheExpirationSeconds => 120;
        public IReadOnlyCollection<string>? CacheTags => [$"provider:{Id}"];
    }

    public sealed record GetSalonUncached(Guid Id) : IQuery<Salon?>;

    public sealed record SameKeyA(Guid Id) : IQuery<Salon?>
    {
        public bool IsCacheable => true;
        public string? CacheKey => "same";
    }

    public sealed record SameKeyB(Guid Id) : IQuery<Salon?>
    {
        public bool IsCacheable => true;
        public string? CacheKey => "same";
    }

    public sealed record SaveSalon(Guid Id) : IRequest<Salon?>;

    private readonly ServiceProvider _services = new ServiceCollection()
        .AddLogging()
        .AddAsanRezerveCaching(new ConfigurationBuilder()
            .AddInMemoryCollection([new("Cache:Provider", "InMemory")])
            .Build())
        .BuildServiceProvider();

    private HybridCache Cache => _services.GetRequiredService<HybridCache>();

    private CacheMetrics Metrics => _services.GetRequiredService<CacheMetrics>();

    private CachingBehavior<TRequest, Salon?> Behavior<TRequest>(HybridCache? cache = null, params ICacheKeyContributor[] contributors)
        where TRequest : class, IRequest<Salon?> =>
        new(cache ?? Cache, Metrics, contributors, NullLogger<CachingBehavior<TRequest, Salon?>>.Instance);

    private static readonly AsyncLocal<string?> Ambient = new();

    private sealed class FixedContributor(string value) : ICacheKeyContributor
    {
        public string? Contribute() => value;
    }

    private sealed class CountingHandler
    {
        public int Calls;
        public Func<Salon?> Result = () => new Salon(Guid.NewGuid(), "نهال");

        public RequestHandlerDelegate<Salon?> Next => _ =>
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(Result());
        };
    }

    public async ValueTask DisposeAsync() => await _services.DisposeAsync();

    [Fact]
    public async Task ACommand_IsNeverCached()
    {
        var handler = new CountingHandler();

        await Behavior<SaveSalon>().Handle(new SaveSalon(Guid.NewGuid()), handler.Next, CancellationToken.None);
        await Behavior<SaveSalon>().Handle(new SaveSalon(Guid.NewGuid()), handler.Next, CancellationToken.None);

        handler.Calls.Should().Be(2);
    }

    [Fact]
    public async Task AQueryThatDoesNotOptIn_IsNeverCached()
    {
        var handler = new CountingHandler();
        var query = new GetSalonUncached(Guid.NewGuid());

        await Behavior<GetSalonUncached>().Handle(query, handler.Next, CancellationToken.None);
        await Behavior<GetSalonUncached>().Handle(query, handler.Next, CancellationToken.None);

        handler.Calls.Should().Be(2);
    }

    [Fact]
    public async Task ACacheableQuery_IsServedFromCache_TheSecondTime_AndCounted()
    {
        var handler = new CountingHandler();
        var query = new GetSalon(Guid.NewGuid());

        var first = await Behavior<GetSalon>().Handle(query, handler.Next, CancellationToken.None);
        var second = await Behavior<GetSalon>().Handle(query, handler.Next, CancellationToken.None);

        handler.Calls.Should().Be(1);
        second.Should().Be(first);
        var stats = Metrics.Snapshot().Single(r => r.Region == nameof(GetSalon));
        stats.Misses.Should().Be(1);
        stats.Hits.Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentMisses_RunTheHandlerOnce()
    {
        var gate = new TaskCompletionSource<Salon?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        RequestHandlerDelegate<Salon?> slowHandler = _ =>
        {
            Interlocked.Increment(ref calls);
            return gate.Task;
        };
        var query = new GetSalon(Guid.NewGuid());

        var readers = Enumerable.Range(0, 50)
            .Select(_ => Behavior<GetSalon>().Handle(query, slowHandler, CancellationToken.None))
            .ToList();
        gate.SetResult(new Salon(query.Id, "نهال"));
        var results = await Task.WhenAll(readers);

        calls.Should().Be(1, "fifty readers of one cold key must cost the database one query, not fifty");
        results.Should().OnlyContain(r => r!.Id == query.Id);
    }

    [Fact]
    public async Task ANullResult_IsReturned_ButNotCached()
    {
        var handler = new CountingHandler { Result = () => null };
        var query = new GetSalon(Guid.NewGuid());

        (await Behavior<GetSalon>().Handle(query, handler.Next, CancellationToken.None)).Should().BeNull();
        (await Behavior<GetSalon>().Handle(query, handler.Next, CancellationToken.None)).Should().BeNull();

        handler.Calls.Should().Be(2, "a salon that does not exist yet must be findable the moment it does");
    }

    [Fact]
    public async Task AHandlerException_Propagates_AndIsNotCached()
    {
        var calls = 0;
        var boom = new InvalidOperationException("db down");
        RequestHandlerDelegate<Salon?> failing = _ =>
        {
            calls++;
            throw boom;
        };
        var query = new GetSalon(Guid.NewGuid());

        var first = () => Behavior<GetSalon>().Handle(query, failing, CancellationToken.None);
        (await first.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(boom);

        var handler = new CountingHandler();
        await Behavior<GetSalon>().Handle(query, handler.Next, CancellationToken.None);
        handler.Calls.Should().Be(1, "the failure must not have been cached");
    }

    [Fact]
    public async Task InvalidatingTheQuerysTag_MakesTheNextReadAMiss()
    {
        var handler = new CountingHandler();
        var query = new GetSalon(Guid.NewGuid());
        await Behavior<GetSalon>().Handle(query, handler.Next, CancellationToken.None);

        await Cache.RemoveByTagAsync($"provider:{query.Id}");
        await Behavior<GetSalon>().Handle(query, handler.Next, CancellationToken.None);

        handler.Calls.Should().Be(2);
    }

    [Fact]
    public async Task TheKey_CoversEveryParameter_WhenTheQueryGivesNoKeyOfItsOwn()
    {
        var handler = new CountingHandler();
        var id = Guid.NewGuid();

        await Behavior<GetSalon>().Handle(new GetSalon(id, Filter: "a"), handler.Next, CancellationToken.None);
        await Behavior<GetSalon>().Handle(new GetSalon(id, Filter: "b"), handler.Next, CancellationToken.None);

        handler.Calls.Should().Be(2, "two queries that differ in one filter must never share an entry");
    }

    [Fact]
    public async Task TwoQueryTypes_WithTheSameExplicitKey_DoNotCollide()
    {
        var handler = new CountingHandler();

        await Behavior<SameKeyA>().Handle(new SameKeyA(Guid.NewGuid()), handler.Next, CancellationToken.None);
        await Behavior<SameKeyB>().Handle(new SameKeyB(Guid.NewGuid()), handler.Next, CancellationToken.None);

        handler.Calls.Should().Be(2);
    }

    [Fact]
    public async Task EntriesUseTheQuerysAbsoluteLifetime_AndItsTags_PlusARegionTag()
    {
        var cache = new RecordingCache(Cache);
        var query = new GetSalon(Guid.NewGuid());

        await Behavior<GetSalon>(cache).Handle(query, new CountingHandler().Next, CancellationToken.None);

        cache.Options!.Expiration.Should().Be(TimeSpan.FromSeconds(120));
        cache.Options.LocalCacheExpiration.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(120));
        cache.Tags.Should().Contain($"provider:{query.Id}").And.Contain($"query:{nameof(GetSalon)}");
    }

    [Fact]
    public async Task ABrokenCache_FallsBackToTheHandler()
    {
        var cache = new RecordingCache(Cache, fail: new InvalidOperationException("serializer exploded"));
        var handler = new CountingHandler();

        var result = await Behavior<GetSalon>(cache).Handle(new GetSalon(Guid.NewGuid()), handler.Next, CancellationToken.None);

        result.Should().NotBeNull();
        handler.Calls.Should().Be(1);
    }

    /// <summary>Forwards to a real cache, recording what the behavior asked for (or failing on request).</summary>
    private sealed class RecordingCache(HybridCache inner, Exception? fail = null) : HybridCache
    {
        public HybridCacheEntryOptions? Options { get; private set; }

        public List<string>? Tags { get; private set; }

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            Tags = tags?.ToList();
            if (fail is not null) throw fail;
            return inner.GetOrCreateAsync(key, state, factory, options, tags, cancellationToken);
        }

        public override ValueTask SetAsync<T>(
            string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) =>
            inner.SetAsync(key, value, options, tags, cancellationToken);

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) =>
            inner.RemoveAsync(key, cancellationToken);

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
            inner.RemoveByTagAsync(tag, cancellationToken);
    }

    [Fact]
    public async Task TheHandler_RunsWithTheCallersAmbientContext()
    {
        // HybridCache runs the factory on a pool thread without the caller's ExecutionContext. Without flowing it,
        // IHttpContextAccessor was null inside the handler (photo URLs came back relative) and the handler's log
        // events lost the request's trace id.
        Ambient.Value = "request-42";
        string? seen = null;
        RequestHandlerDelegate<Salon?> handler = _ =>
        {
            seen = Ambient.Value;
            return Task.FromResult<Salon?>(new Salon(Guid.NewGuid(), "x"));
        };

        await Behavior<GetSalon>().Handle(new GetSalon(Guid.NewGuid()), handler, CancellationToken.None);

        seen.Should().Be("request-42");
    }

    [Fact]
    public async Task KeyContributors_KeepVariantsApart()
    {
        // e.g. the public base URL photo links are built from, when it comes from the request's host.
        var handler = new CountingHandler();
        var query = new GetSalon(Guid.NewGuid());

        await Behavior<GetSalon>(null, new FixedContributor("http://10.0.0.5:5000")).Handle(query, handler.Next, CancellationToken.None);
        await Behavior<GetSalon>(null, new FixedContributor("http://localhost:5000")).Handle(query, handler.Next, CancellationToken.None);
        await Behavior<GetSalon>(null, new FixedContributor("http://localhost:5000")).Handle(query, handler.Next, CancellationToken.None);

        handler.Calls.Should().Be(2);
    }

    [Fact]
    public async Task CallerCancellation_Propagates()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        RequestHandlerDelegate<Salon?> cancelled = t =>
        {
            t.ThrowIfCancellationRequested();
            return Task.FromResult<Salon?>(null);
        };

        var act = () => Behavior<GetSalon>().Handle(new GetSalon(Guid.NewGuid()), cancelled, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
