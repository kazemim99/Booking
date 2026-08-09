using Booksy.Infrastructure.Core.Caching;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;

namespace Booksy.Infrastructure.Core.UnitTests.Caching;

/// <summary>
/// The cache is an optimization, never a dependency. When Redis is
/// unreachable every call blocks for the connect timeout (5s by default);
/// with four cache operations on the request path that turned a 40ms query
/// into a 22-second request. The breaker bounds that cost: after a few
/// failures the cache is bypassed outright and callers read through to the
/// database.
/// </summary>
public sealed class RedisCacheServiceCircuitBreakerTests
{
    private static readonly RedisConnectionException Unreachable =
        new(ConnectionFailureType.UnableToConnect, "redis down");

    private sealed record Payload(string Value);

    private static RedisCacheService Build(
        out IDatabase database,
        int threshold = 3,
        int resetSeconds = 30)
    {
        var db = Substitute.For<IDatabase>();
        database = db;

        var multiplexer = Substitute.For<IConnectionMultiplexer>();
        multiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(db);

        var settings = Options.Create(new CacheSettings
        {
            Provider = "Redis",
            KeyPrefix = "booksy",
            CircuitBreakerFailureThreshold = threshold,
            CircuitBreakerResetSeconds = resetSeconds,
        });

        return new RedisCacheService(
            multiplexer, settings, NullLogger<RedisCacheService>.Instance);
    }

    private static void FailsToConnect(IDatabase db) =>
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(Unreachable);

    [Fact]
    public async Task GetAsync_ReturnsNullInsteadOfThrowing_WhenRedisIsDown()
    {
        var sut = Build(out var db);
        FailsToConnect(db);

        var result = await sut.GetAsync<Payload>("k");

        result.Should().BeNull("a cache failure must degrade, not surface to the caller");
    }

    [Fact]
    public async Task RepeatedFailures_OpenTheCircuit_AndStopTouchingRedis()
    {
        var sut = Build(out var db, threshold: 3);
        FailsToConnect(db);

        // Three failures trip the breaker.
        for (var i = 0; i < 3; i++)
            await sut.GetAsync<Payload>("k");

        sut.ConsecutiveFailures.Should().Be(3);
        db.ClearReceivedCalls();

        // Further calls short-circuit: Redis is never contacted, so no caller
        // pays the connect timeout again.
        for (var i = 0; i < 5; i++)
            (await sut.GetAsync<Payload>("k")).Should().BeNull();

        await db.DidNotReceive().StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task OpenCircuit_AlsoBypassesWrites_AndExistenceChecks()
    {
        var sut = Build(out var db, threshold: 1);
        FailsToConnect(db);
        await sut.GetAsync<Payload>("k"); // trips at threshold 1
        db.ClearReceivedCalls();

        await sut.SetAsync("k", new Payload("v"));
        await sut.RemoveAsync("k");
        await sut.RefreshAsync("k");
        var exists = await sut.ExistsAsync("k");

        exists.Should().BeFalse();
        await db.DidNotReceive().StringSetAsync(
            Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(), Arg.Any<When>(), Arg.Any<CommandFlags>());
        await db.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        await db.DidNotReceive().KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task GetOrAddAsync_StillReturnsFreshValue_WhileTheCircuitIsOpen()
    {
        var sut = Build(out var db, threshold: 1);
        FailsToConnect(db);
        await sut.GetAsync<Payload>("k"); // open the circuit

        var factoryCalls = 0;
        var value = await sut.GetOrAddAsync("k", () =>
        {
            factoryCalls++;
            return Task.FromResult(new Payload("from-source"));
        });

        // The whole point: a dead cache must not break the read path.
        value.Value.Should().Be("from-source");
        factoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task CircuitReopensForATrialRequest_OnceTheCooldownElapses()
    {
        // Zero cooldown ⇒ the next call is immediately allowed through.
        var sut = Build(out var db, threshold: 2, resetSeconds: 0);
        FailsToConnect(db);
        await sut.GetAsync<Payload>("k");
        await sut.GetAsync<Payload>("k");
        sut.ConsecutiveFailures.Should().Be(2);
        db.ClearReceivedCalls();

        await sut.GetAsync<Payload>("k");

        await db.Received(1).StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SuccessAfterRecovery_ClosesTheCircuit()
    {
        var sut = Build(out var db, threshold: 2, resetSeconds: 0);
        FailsToConnect(db);
        await sut.GetAsync<Payload>("k");
        await sut.GetAsync<Payload>("k");
        sut.ConsecutiveFailures.Should().Be(2);

        // Redis comes back: the trial request succeeds and resets the count.
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        await sut.GetAsync<Payload>("k");

        sut.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public async Task HealthyRedis_IsNeverBypassed()
    {
        var sut = Build(out var db);
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);

        for (var i = 0; i < 10; i++)
            await sut.GetAsync<Payload>("k");

        sut.ConsecutiveFailures.Should().Be(0);
        await db.Received(10).StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }
}
