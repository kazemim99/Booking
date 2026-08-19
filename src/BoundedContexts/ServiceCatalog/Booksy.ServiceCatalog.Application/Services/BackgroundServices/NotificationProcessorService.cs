// ========================================
// Booksy.ServiceCatalog.Application/Services/BackgroundServices/NotificationProcessorService.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Services.BackgroundServices
{
    /// <summary>
    /// Background service that drains queued notifications and retries failed ones.
    /// </summary>
    /// <remarks>
    /// This is the backstop behind the durable-retry event: whatever the outbox misses (a process that died
    /// mid-dispatch, a bus that was unavailable when the failure happened) is still sitting in the Notifications
    /// table and gets picked up here. It delegates to <see cref="INotificationDispatcher"/> rather than fanning
    /// out to the channel services itself, so preferences, de-duplication and dead-lettering apply identically to
    /// the retry path and the request path.
    /// </remarks>
    public sealed class NotificationProcessorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationProcessorService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

        public NotificationProcessorService(
            IServiceProvider serviceProvider,
            ILogger<NotificationProcessorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationProcessorService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessQueuedNotificationsAsync(stoppingToken);
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notifications");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("NotificationProcessorService stopped");
        }

        private async Task ProcessQueuedNotificationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationWriteRepository>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

            // Queued, pending, and failed-but-retryable notifications
            var notifications = await notificationRepository.GetPendingNotificationsAsync(100, cancellationToken);

            if (notifications.Count == 0)
                return;

            _logger.LogInformation("Processing {Count} pending notifications", notifications.Count);

            foreach (var notification in notifications)
            {
                try
                {
                    var statusBefore = notification.Status;
                    var outcome = await dispatcher.DispatchAsync(notification, cancellationToken);

                    // Notifications whose backoff has not elapsed (or that are not due yet) come back untouched;
                    // persisting them would be a pointless write on every sweep.
                    if (notification.Status == statusBefore && outcome.EntirelySkipped && outcome.Skipped == 0)
                        continue;

                    await notificationRepository.UpdateNotificationAsync(notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    // The dispatcher reports gateway failures instead of throwing, so reaching here means
                    // something structural went wrong for this notification. Never let it stop the batch.
                    _logger.LogError(ex,
                        "Failed to process notification {NotificationId}",
                        notification.Id.Value);
                }
            }
        }
    }
}
