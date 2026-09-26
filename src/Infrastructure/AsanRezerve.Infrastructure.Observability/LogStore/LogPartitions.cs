using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AsanRezerve.Infrastructure.Observability.LogStore;

/// <summary>What one retention run removed.</summary>
public sealed record LogRetentionResult(IReadOnlyList<string> DroppedPartitions, long DeletedParkedEvents);

/// <summary>Size of the log store, for the admin overview: the number to watch before storing less.</summary>
public sealed record LogStorage(long TotalBytes, int Partitions, DateOnly? OldestDay, DateOnly? NewestDay);

/// <summary>
/// Maintains the daily partitions of <c>observability.log_events</c> (design D6, plan in <see cref="LogPartitionPlan"/>).
/// <para>Creating a day moves any of its events already parked in the default partition into it, in the same
/// transaction (Postgres refuses to create a partition whose rows sit in the default one). Retention drops whole days —
/// instant, and no dead rows for vacuum — and deletes the few parked events past the retention. Every change runs under
/// one advisory lock, so two hosts on one database never race to create the same day.</para>
/// </summary>
public sealed class LogPartitions(LogStoreDataSource dataSource)
{
    private const string Parent = "observability.log_events";
    private const string Default = "observability." + LogPartitionPlan.DefaultPartition;
    private const long LockKey = 0x_6C6F_675F_7061_7274; // "log_part"

    /// <summary>The partitions of <c>log_events</c>, the default one included.</summary>
    public async Task<IReadOnlyList<string>> PartitionNamesAsync(CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.Value.CreateCommand(
            "SELECT c.relname FROM pg_inherits i JOIN pg_class c ON c.oid = i.inhrelid " +
            $"WHERE i.inhparent = '{Parent}'::regclass ORDER BY 1");
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) names.Add(reader.GetString(0));
        return names;
    }

    /// <summary>Creates the missing days around <paramref name="now"/>; returns how many were created.</summary>
    public async Task<int> EnsureAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var missing = LogPartitionPlan.ToCreate(now, await PartitionNamesAsync(cancellationToken));
        var created = 0;
        foreach (var day in missing)
        {
            if (await CreateAsync(day, cancellationToken)) created++;
        }
        return created;
    }

    /// <summary>Drops the days past the retention and deletes parked events past it.</summary>
    public async Task<LogRetentionResult> ApplyRetentionAsync(
        DateTimeOffset now, int retentionDays, CancellationToken cancellationToken = default)
    {
        var drop = LogPartitionPlan.ToDrop(now, await PartitionNamesAsync(cancellationToken), retentionDays);

        await using var connection = await dataSource.Value.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await LockAsync(connection, cancellationToken);

        foreach (var name in drop)
        {
            // Names come from LogPartitionPlan (log_events_pYYYYMMDD), never from input.
            await ExecuteAsync(connection, $"DROP TABLE IF EXISTS observability.{name}", cancellationToken);
        }

        await using var delete = new NpgsqlCommand($"DELETE FROM {Default} WHERE \"timestamp\" < $1", connection);
        delete.Parameters.Add(new NpgsqlParameter { Value = LogPartitionPlan.Cutoff(now, retentionDays) });
        var deleted = await delete.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new LogRetentionResult(drop, deleted);
    }

    public async Task<LogStorage> StorageAsync(CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.Value.CreateCommand(
            "SELECT c.relname, pg_total_relation_size(c.oid) FROM pg_inherits i JOIN pg_class c ON c.oid = i.inhrelid " +
            $"WHERE i.inhparent = '{Parent}'::regclass");
        long total = 0;
        var days = new List<DateOnly>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            total += reader.GetInt64(1);
            if (LogPartitionPlan.TryParseDay(reader.GetString(0), out var day)) days.Add(day);
        }
        return new LogStorage(total, days.Count, days.Count == 0 ? null : days.Min(), days.Count == 0 ? null : days.Max());
    }

    private async Task<bool> CreateAsync(DateOnly day, CancellationToken cancellationToken)
    {
        var (from, to) = LogPartitionPlan.Bounds(day);
        var name = "observability." + LogPartitionPlan.NameOf(day);

        await using var connection = await dataSource.Value.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await LockAsync(connection, cancellationToken);

        await using (var exists = new NpgsqlCommand("SELECT to_regclass($1) IS NOT NULL", connection))
        {
            exists.Parameters.Add(new NpgsqlParameter { Value = name });
            if ((bool)(await exists.ExecuteScalarAsync(cancellationToken))!) return false; // another host was first
        }

        // Park → temp table → create the day → put them back. Bounds are formatted by us from a DateOnly.
        var range = $"\"timestamp\" >= '{Utc(from)}' AND \"timestamp\" < '{Utc(to)}'";
        await ExecuteAsync(connection,
            $"CREATE TEMP TABLE parked ON COMMIT DROP AS SELECT * FROM {Default} WHERE {range}", cancellationToken);
        await ExecuteAsync(connection, $"DELETE FROM {Default} WHERE {range}", cancellationToken);
        await ExecuteAsync(connection,
            $"CREATE TABLE {name} PARTITION OF {Parent} " +
            $"FOR VALUES FROM ('{Utc(from)}') TO ('{Utc(to)}')", cancellationToken);
        await ExecuteAsync(connection, $"INSERT INTO {Parent} OVERRIDING SYSTEM VALUE SELECT * FROM parked", cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static string Utc(DateTimeOffset at) =>
        at.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static async Task LockAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock($1)", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = LockKey });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

/// <summary>Hourly: creates the coming days' partitions and applies the retention (<see cref="LogPartitions"/>).</summary>
public sealed class LogPartitionMaintenanceService(
    LogPartitions partitions,
    LogStoreWriter writer,
    IOptions<LogStoreOptions> options,
    TimeProvider time) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1), time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!writer.Stats.Ready) continue;

            try
            {
                var now = time.GetUtcNow();
                await partitions.EnsureAsync(now, stoppingToken);
                await partitions.ApplyRetentionAsync(now, options.Value.RetentionDays, stoppingToken);
            }
            catch (Exception) when (!stoppingToken.IsCancellationRequested)
            {
                // Logged nowhere on purpose (see LogStoreWriter); the next hour retries. Events without a day
                // partition wait in the default one meanwhile; the overview shows the store's size.
            }
        }
    }
}
