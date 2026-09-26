using AsanRezerve.Infrastructure.Core.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;

namespace AsanRezerve.Infrastructure.Core.UnitTests.Caching;

/// <summary>
/// The cache is an optimization, never a dependency. When Redis is unreachable every call blocks for the connect
/// timeout; with several cache operations on the request path that once turned a 40ms query into a 22-second
/// request. The breaker bounds that cost: after a few failures the second-level cache is bypassed outright and
/// HybridCache reads through to its in-process tier and the database.
/// <para>These are the scenarios the retired <c>RedisCacheService</c> breaker was held to, moved to the decorator
/// that now guards HybridCache's L2.</para>
/// </summary>
public sealed class ResilientDistributedCacheTests
{
    private static readonly RedisConnectionException Unreachable =
        new(ConnectionFailureType.UnableToConnect, "redis down");

    private static readonly byte[] Value = [1, 2, 3];

    private static ResilientDistributedCache Build(
        out IDistributedCache inner,
        int threshold = 3,
        int resetSeconds = 30,
        ManualTimeProvider? time = null)
    {
        inner = Substitute.For<IDistributedCache>();

        var settings = Options.Create(new CacheSettings
        {
            CircuitBreakerFailureThreshold = threshold,
            CircuitBreakerResetSeconds = resetSeconds,
        });

        return new ResilientDistributedCache(
            inner, settings, NullLogger<ResilientDistributedCache>.Instance, time ?? new ManualTimeProvider());
    }

    private static void FailsToConnect(IDistributedCache inner) =>
        inner.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(Unreachable);

    [Fact]
    public async Task GetAsync_ReturnsNullInsteadOfThrowing_WhenRedisIsDown()
    {
        var sut = Build(out var inner);
        FailsToConnect(inner);

        var result = await sut.GetAsync("k");

        result.Should().BeNull("a cache failure must degrade to a miss, not surface to the caller");
    }

    [Fact]
    public async Task RepeatedFailures_OpenTheCircuit_AndStopTouchingRedis()
    {
        var sut = Build(out var inner, threshold: 3);
        FailsToConnect(inner);

        for (var i = 0; i < 3; i++)
            await sut.GetAsync("k");

        sut.ConsecutiveFailures.Should().Be(3);
        sut.State.Should().Be(CircuitState.Open);
        inner.ClearReceivedCalls();

        // Further calls short-circuit: Redis is never contacted, so no caller pays the timeout again.
        for (var i = 0; i < 5; i++)
            (await sut.GetAsync("k")).Should().BeNull();

        await inner.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenCircuit_AlsoBypassesWrites_RefreshesAndRemoves()
    {
        var sut = Build(out var inner, threshold: 1);
        FailsToConnect(inner);
        await sut.GetAsync("k"); // trips at threshold 1
        inner.ClearReceivedCalls();

        await sut.SetAsync("k", Value, new DistributedCacheEntryOptions());
        await sut.RefreshAsync("k");
        await sut.RemoveAsync("k");
        sut.Set("k", Value, new DistributedCacheEntryOptions());
        sut.Get("k").Should().BeNull();

        inner.ReceivedCalls().Should().BeEmpty("an open circuit must not touch Redis for any operation");
    }

    [Fact]
    public async Task WriteFailures_AreSwallowed_AndCounted()
    {
        var sut = Build(out var inner, threshold: 5);
        inner.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(Unreachable);

        var act = () => sut.SetAsync("k", Value, new DistributedCacheEntryOptions());

        await act.Should().NotThrowAsync();
        sut.ConsecutiveFailures.Should().Be(1);
        sut.LastError.Should().Contain("redis down");
    }

    [Fact]
    public async Task CircuitLetsATrialRequestThrough_OnceTheCooldownElapses()
    {
        var time = new ManualTimeProvider();
        var sut = Build(out var inner, threshold: 2, resetSeconds: 30, time: time);
        FailsToConnect(inner);
        await sut.GetAsync("k");
        await sut.GetAsync("k");
        inner.ClearReceivedCalls();

        time.Advance(TimeSpan.FromSeconds(29));
        await sut.GetAsync("k");
        await inner.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        time.Advance(TimeSpan.FromSeconds(2));
        sut.State.Should().Be(CircuitState.HalfOpen);
        await sut.GetAsync("k");

        await inner.Received(1).GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailedTrial_ReopensTheCircuit_ForAnotherCooldown()
    {
        var time = new ManualTimeProvider();
        var sut = Build(out var inner, threshold: 2, resetSeconds: 30, time: time);
        FailsToConnect(inner);
        await sut.GetAsync("k");
        await sut.GetAsync("k");

        time.Advance(TimeSpan.FromSeconds(31));
        await sut.GetAsync("k"); // the trial fails
        inner.ClearReceivedCalls();

        await sut.GetAsync("k");

        sut.State.Should().Be(CircuitState.Open);
        await inner.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuccessAfterRecovery_ClosesTheCircuit()
    {
        var time = new ManualTimeProvider();
        var sut = Build(out var inner, threshold: 2, resetSeconds: 30, time: time);
        FailsToConnect(inner);
        await sut.GetAsync("k");
        await sut.GetAsync("k");

        // Redis comes back: the trial request succeeds and resets the count.
        time.Advance(TimeSpan.FromSeconds(31));
        inner.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        await sut.GetAsync("k");

        sut.ConsecutiveFailures.Should().Be(0);
        sut.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task HealthyRedis_IsNeverBypassed()
    {
        var sut = Build(out var inner);
        inner.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Value);

        for (var i = 0; i < 10; i++)
            (await sut.GetAsync("k")).Should().Equal(Value);

        sut.ConsecutiveFailures.Should().Be(0);
        await inner.Received(10).GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CallerCancellation_IsNotARedisFailure()
    {
        var sut = Build(out var inner, threshold: 1);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        inner.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var act = () => sut.GetAsync("k", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        sut.ConsecutiveFailures.Should().Be(0, "a caller giving up says nothing about Redis' health");
    }
}

/// <summary>A clock a test moves by hand; the breaker reads time only through <see cref="TimeProvider"/>.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
