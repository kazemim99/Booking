using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Looks in on <see cref="DailyScheduleDigestJob"/> often enough to catch each morning.
    /// </summary>
    /// <remarks>
    /// <para>The interval is minutes rather than seconds because the job is only ever a no-op outside its
    /// morning window, and being a few minutes late to a daily digest is invisible — unlike the outbox sweep
    /// beside it, which bounds how late a booking confirmation is and therefore runs every fifteen seconds.</para>
    ///
    /// <para>Several hosts may run this at once. Nothing coordinates them and nothing needs to: the outbox's
    /// unique index on the digest's per-salon-per-day key is what makes a second host's pass a no-op.</para>
    /// </remarks>
    public sealed class DailyScheduleDigestService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

        /// <summary>Back off on failure rather than retry at full speed against something that is down.</summary>
        private static readonly TimeSpan PauseAfterFailure = TimeSpan.FromMinutes(30);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyScheduleDigestService> _logger;

        public DailyScheduleDigestService(
            IServiceProvider serviceProvider,
            ILogger<DailyScheduleDigestService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyScheduleDigestService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var job = scope.ServiceProvider.GetRequiredService<DailyScheduleDigestJob>();
                        await job.ExecuteAsync(stoppingToken);
                    }

                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Daily schedule digest pass failed; pausing before the next one");

                    try
                    {
                        await Task.Delay(PauseAfterFailure, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("DailyScheduleDigestService stopped");
        }
    }
}
