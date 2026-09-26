using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AsanRezerve.Infrastructure.Observability.LogStore;

/// <summary>Deletes stored events older than <see cref="LogStoreOptions.RetentionDays"/> (14), in chunks.</summary>
public sealed class LogRetention(LogStoreDataSource dataSource)
{
    private const int Chunk = 10_000;

    /// <summary>Deletes every event older than <paramref name="cutoff"/>; returns how many.</summary>
    public async Task<long> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        long total = 0;
        while (true)
        {
            await using var command = dataSource.Value.CreateCommand(
                "DELETE FROM observability.log_events WHERE id IN " +
                "(SELECT id FROM observability.log_events WHERE timestamp < $1 ORDER BY id LIMIT $2)");
            command.Parameters.Add(new NpgsqlParameter { Value = cutoff.ToUniversalTime() });
            command.Parameters.Add(new NpgsqlParameter { Value = Chunk });

            var deleted = await command.ExecuteNonQueryAsync(cancellationToken);
            total += deleted;
            if (deleted < Chunk) return total;
        }
    }
}

/// <summary>Runs <see cref="LogRetention"/> an hour after startup and hourly after that.</summary>
public sealed class LogRetentionService(
    LogRetention retention,
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
                var cutoff = time.GetUtcNow() - TimeSpan.FromDays(Math.Max(1, options.Value.RetentionDays));
                await retention.DeleteOlderThanAsync(cutoff, stoppingToken);
            }
            catch (Exception) when (!stoppingToken.IsCancellationRequested)
            {
                // Logged nowhere on purpose (see LogStoreWriter); the next hour retries. Row counts are on the overview.
            }
        }
    }
}
