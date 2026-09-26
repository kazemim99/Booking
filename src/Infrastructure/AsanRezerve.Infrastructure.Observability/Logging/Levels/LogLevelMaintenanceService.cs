using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.Infrastructure.Observability.Logging.Levels;

/// <summary>Re-applies persisted overrides at startup, then expires temporary ones every 30 seconds.</summary>
public sealed class LogLevelMaintenanceService(LogLevelService levels, TimeProvider time, ILogger<LogLevelMaintenanceService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await levels.LoadAsync(stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Could not re-apply persisted log-level overrides; configured levels apply");
        }

        using var timer = new PeriodicTimer(Interval, time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await levels.ExpireDueAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Expiring log-level overrides failed; retrying in {Seconds}s", Interval.TotalSeconds);
            }
        }
    }
}
