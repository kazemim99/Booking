using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Idempotency;
using Booksy.Core.Application.Behaviors;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Payments;

/// <summary>
/// C2 §2 atomic idempotency reservation, against a real PostgreSQL. Proves the composite-PK reservation is the
/// serialization point: reserve/in-flight/completed transitions, release-for-retry (fault injection), stale reclaim
/// (crash recovery), and — the key guarantee — exactly one winner under concurrent identical requests.
/// </summary>
public class IdempotencyStoreTests : Infrastructure.ServiceCatalogIntegrationTestBase
{
    public IdempotencyStoreTests(Infrastructure.ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory) { }

    private IIdempotencyStore Store(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();
    private static readonly TimeSpan FiveMin = TimeSpan.FromMinutes(5);

    public sealed record IdemCmd(Guid? IdempotencyKey) : ICommand<string>, IRequireIdempotency;

    [Fact]
    public async Task End_to_end_behavior_with_the_real_store_runs_the_handler_once_then_replays_the_stored_result()
    {
        using var scope = Factory.Services.CreateScope();
        // Real IServiceProvider → resolves the real EF-backed IIdempotencyStore.
        var behavior = new IdempotencyBehavior<IdemCmd, string>(
            scope.ServiceProvider, NullLogger<IdempotencyBehavior<IdemCmd, string>>.Instance);

        var key = Guid.NewGuid();
        var calls = 0;
        RequestHandlerDelegate<string> next = _ => { calls++; return Task.FromResult($"charged-{calls}"); };

        var first = await behavior.Handle(new IdemCmd(key), next, CancellationToken.None);
        var second = await behavior.Handle(new IdemCmd(key), next, CancellationToken.None);

        calls.Should().Be(1, "the handler (and thus the gateway) runs exactly once for a repeated idempotency key");
        first.Should().Be("charged-1");
        second.Should().Be("charged-1", "the retry replays the stored result, never a fresh execution");
    }

    [Fact]
    public async Task Fresh_key_reserves_then_duplicate_is_in_flight_then_completed_replays()
    {
        var key = Guid.NewGuid().ToString("N");
        using var scope = Factory.Services.CreateScope();
        var store = Store(scope);

        (await store.TryReserveAsync("Cmd", key, FiveMin)).State.Should().Be(IdempotencyState.Reserved);
        (await store.TryReserveAsync("Cmd", key, FiveMin)).State.Should().Be(IdempotencyState.InFlight, "a concurrent duplicate must not win");

        await store.CompleteAsync("Cmd", key, "\"the-result\"");
        var replay = await store.TryReserveAsync("Cmd", key, FiveMin);
        replay.State.Should().Be(IdempotencyState.Completed);
        replay.ResultJson.Should().Be("\"the-result\"");
    }

    [Fact]
    public async Task Release_lets_a_failed_request_be_retried()
    {
        var key = Guid.NewGuid().ToString("N");
        using var scope = Factory.Services.CreateScope();
        var store = Store(scope);

        (await store.TryReserveAsync("Cmd", key, FiveMin)).State.Should().Be(IdempotencyState.Reserved);
        await store.ReleaseAsync("Cmd", key); // handler failed → release
        (await store.TryReserveAsync("Cmd", key, FiveMin)).State.Should().Be(IdempotencyState.Reserved, "a released key is reservable again");
    }

    [Fact]
    public async Task A_stale_in_flight_reservation_is_reclaimed()
    {
        var key = Guid.NewGuid().ToString("N");
        using var scope = Factory.Services.CreateScope();
        var store = Store(scope);
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

        (await store.TryReserveAsync("Cmd", key, FiveMin)).State.Should().Be(IdempotencyState.Reserved);

        // Simulate the processor crashing mid-flight: backdate the reservation beyond the staleness window.
        await db.Database.ExecuteSqlRawAsync(
            @"UPDATE ""ServiceCatalog"".""IdempotencyReservations"" SET ""CreatedAt"" = now() - interval '10 minutes'
              WHERE ""RequestType"" = {0} AND ""Key"" = {1}", "Cmd", key);

        (await store.TryReserveAsync("Cmd", key, FiveMin)).State
            .Should().Be(IdempotencyState.Reserved, "a stale in-flight reservation is reclaimed so a key never sticks forever");
    }

    [Fact]
    public async Task Concurrent_reserves_of_the_same_key_yield_exactly_one_winner()
    {
        var key = Guid.NewGuid().ToString("N");
        using var scope = Factory.Services.CreateScope();
        var store = Store(scope); // store is stateless (each op opens its own scope) → safe to share across tasks

        const int n = 24;
        var tasks = Enumerable.Range(0, n)
            .Select(_ => Task.Run(() => store.TryReserveAsync("Cmd", key, FiveMin)))
            .ToArray();
        var outcomes = await Task.WhenAll(tasks);

        outcomes.Count(o => o.State == IdempotencyState.Reserved)
            .Should().Be(1, "exactly one concurrent caller may reserve the key — the rest see it in-flight");
        outcomes.Count(o => o.State == IdempotencyState.InFlight).Should().Be(n - 1);

        using var verify = Factory.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        (await db.IdempotencyReservations.CountAsync(r => r.Key == key)).Should().Be(1, "only one reservation row exists");
    }
}
