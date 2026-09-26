using AsanRezerve.Infrastructure.Observability.LogStore;
using AsanRezerve.ServiceCatalog.IntegrationTests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace AsanRezerve.Host.IntegrationTests.Observability;

/// <summary>
/// <c>observability.log_events</c> is partitioned by UTC day (log-explorer: "Stored logs are kept for 14 days"):
/// retention drops whole days instead of deleting rows, and an event with no day partition waits in the default
/// partition until its day is created. Against the real migration and Postgres; rows are written straight through the
/// batch writer with chosen timestamps and told apart by a unique marker.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class LogPartitionTests : ServiceCatalogIntegrationTestBase
{
    private const string DefaultPartition = "observability." + LogPartitionPlan.DefaultPartition;

    public LogPartitionTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private LogPartitions Partitions => Factory.Services.GetRequiredService<LogPartitions>();

    private NpgsqlDataSource Database => Factory.Services.GetRequiredService<LogStoreDataSource>().Value;

    private static string Marker() => "part-" + Guid.NewGuid().ToString("N")[..12];

    private static string Qualified(DateOnly day) => "observability." + LogPartitionPlan.NameOf(day);

    private static DateOnly Day(DateTimeOffset at) => DateOnly.FromDateTime(at.UtcDateTime);

    private Task WriteAsync(params (DateTimeOffset At, string Message)[] rows) =>
        Factory.Services.GetRequiredService<ILogEventBatchWriter>().WriteAsync(
            rows.Select(r => new LogEventRow(r.At, 3, r.Message, "x", null, "AsanRezerve.Tests.Partitions",
                null, null, null, null, null, null, null, null)).ToList(),
            CancellationToken.None);

    /// <summary>The partition a message is stored in, or null when it is gone.</summary>
    private async Task<string?> PartitionOfAsync(string message)
    {
        await using var command = Database.CreateCommand(
            "SELECT tableoid::regclass::text FROM observability.log_events WHERE message = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = message });
        return (string?)await command.ExecuteScalarAsync();
    }

    [Fact]
    public async Task The_log_table_is_partitioned_by_day_from_startup()
    {
        await using var kind = Database.CreateCommand(
            "SELECT c.relkind::text FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace " +
            "WHERE n.nspname = 'observability' AND c.relname = 'log_events'");
        ((string?)await kind.ExecuteScalarAsync()).Should().Be("p");

        var now = DateTimeOffset.UtcNow;
        var names = await Partitions.PartitionNamesAsync();
        names.Should().Contain([LogPartitionPlan.NameOf(Day(now)), LogPartitionPlan.NameOf(Day(now).AddDays(2)), LogPartitionPlan.DefaultPartition]);

        var message = "today " + Marker();
        await WriteAsync((now, message));
        (await PartitionOfAsync(message)).Should().Be(Qualified(Day(now)));
    }

    [Fact]
    public async Task Ensuring_the_partitions_again_creates_nothing()
    {
        var now = DateTimeOffset.UtcNow;
        await Partitions.EnsureAsync(now);

        (await Partitions.EnsureAsync(now)).Should().Be(0);
    }

    [Fact]
    public async Task An_event_parked_in_the_default_partition_moves_into_its_day_when_the_day_is_created()
    {
        var day = Day(DateTimeOffset.UtcNow).AddDays(90);
        var at = LogPartitionPlan.Bounds(day).From.AddHours(12);
        var message = "future " + Marker();
        try
        {
            await WriteAsync((at, message));
            (await PartitionOfAsync(message)).Should().Be(DefaultPartition);

            (await Partitions.EnsureAsync(at)).Should().BeGreaterThan(0);

            (await PartitionOfAsync(message)).Should().Be(Qualified(day));
        }
        finally
        {
            foreach (var offset in Enumerable.Range(-1, 4))
            {
                await using var drop = Database.CreateCommand($"DROP TABLE IF EXISTS {Qualified(day.AddDays(offset))}");
                await drop.ExecuteNonQueryAsync();
            }
        }
    }

    [Fact]
    public async Task Retention_drops_old_days_and_old_parked_events_and_keeps_recent_ones()
    {
        var now = DateTimeOffset.UtcNow;
        var marker = Marker();
        var longAgo = now.AddDays(-30);
        await Partitions.EnsureAsync(longAgo); // the days around 30 days ago get partitions of their own
        var inOldDay = "old-day " + marker;
        var parked = "old-parked " + marker;   // 15 days ago has no partition: it waits in the default one
        var recent = "recent " + marker;
        await WriteAsync((longAgo, inOldDay), (now.AddDays(-15), parked), (now.AddDays(-1), recent));
        (await PartitionOfAsync(inOldDay)).Should().Be(Qualified(Day(longAgo)));
        (await PartitionOfAsync(parked)).Should().Be(DefaultPartition);

        var result = await Partitions.ApplyRetentionAsync(now, retentionDays: 14);

        result.DroppedPartitions.Should().Contain(LogPartitionPlan.NameOf(Day(longAgo)));
        result.DeletedParkedEvents.Should().BeGreaterThanOrEqualTo(1);
        (await Partitions.PartitionNamesAsync()).Should().NotContain(LogPartitionPlan.NameOf(Day(longAgo)))
            .And.Contain(LogPartitionPlan.NameOf(Day(now)));
        (await PartitionOfAsync(inOldDay)).Should().BeNull();
        (await PartitionOfAsync(parked)).Should().BeNull();
        (await PartitionOfAsync(recent)).Should().Be(Qualified(Day(now.AddDays(-1))));
    }
}
