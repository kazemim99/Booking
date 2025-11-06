// ========================================
// Booksy.ServiceCatalog.Application/Services/BackgroundServices/NotificationCleanupService.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Services.BackgroundServices
{
    /// <summary>
    /// Background service that cleans up expired notifications
    /// </summary>
    public sealed class NotificationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public NotificationCleanupService(
            IServiceProvider serviceProvider,
            ILogger<NotificationCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationCleanupService started");

            // Wait 5 minutes before first cleanup
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredNotificationsAsync(stoppingToken);
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up expired notifications");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("NotificationCleanupService stopped");
        }

        private async Task CleanupExpiredNotificationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationWriteRepository>();

            var currentTime = DateTime.UtcNow;

            // Get expired notifications
            var notifications = await notificationRepository.GetExpiredNotificationsAsync(
                currentTime,
                500,
                cancellationToken);

            if (!notifications.Any())
                return;

            _logger.LogInformation(
                "Cleaning up {Count} expired notifications",
                notifications.Count);

            int cleaned = 0;
            foreach (var notification in notifications)
            {
                try
                {
                    // Mark as failed with expiration reason
                    if (!notification.IsExpired())
                        continue;

                    notification.MarkAsFailed("Notification expired before delivery");
                    await notificationRepository.UpdateNotificationAsync(notification, cancellationToken);
                    cleaned++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to cleanup expired notification {NotificationId}",
                        notification.Id.Value);
                }
            }

            _logger.LogInformation("Cleaned up {Cleaned} expired notifications", cleaned);
        }
    }
}
