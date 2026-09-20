using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Runs the notification outbox sweep on a loop.
    /// </summary>
    /// <remarks>
    /// <para>Follows the shape of the other notification background services: its own DI scope per pass, a
    /// fixed interval, and a longer pause after a failure so a broken dependency is not hammered once a
    /// minute.</para>
    ///
    /// <para>It shares the database with <c>ScheduledNotificationService</c> but not its rows: that one sweeps
    /// <c>Notifications</c> that are already queued, this one sweeps <c>NotificationOutbox</c> intents that
    /// have not become notifications yet. Running both is safe — this sweep's output is that sweep's input.</para>
    ///
    /// <para>Several hosts may run this at once. That is the reason the claim is a database-level
    /// <c>FOR UPDATE SKIP LOCKED</c> and not an in-process lock.</para>
    /// </remarks>
    public sealed class NotificationOutboxService : BackgroundService
    {
        /// <summary>
        /// Short, because it bounds how late an immediate notification is. A booking confirmation that waits
        /// a minute reads as a broken app.
        /// </summary>
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

        /// <summary>Back off on failure rather than retry at full speed against something that is down.</summary>
        private static readonly TimeSpan PauseAfterFailure = TimeSpan.FromMinutes(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationOutboxService> _logger;

        public NotificationOutboxService(
            IServiceProvider serviceProvider,
            ILogger<NotificationOutboxService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationOutboxService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var job = scope.ServiceProvider.GetRequiredService<ProcessNotificationOutboxJob>();
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
                    // The job already handles a single row failing; reaching here means something broader —
                    // the database is unreachable, or a dependency failed to resolve.
                    _logger.LogError(ex, "Notification outbox sweep failed; pausing before the next pass");

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

            _logger.LogInformation("NotificationOutboxService stopped");
        }
    }
}
