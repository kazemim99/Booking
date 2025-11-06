// ========================================
// Booksy.ServiceCatalog.Application/Services/BackgroundServices/ScheduledNotificationService.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Services.BackgroundServices
{
    /// <summary>
    /// Background service that processes scheduled notifications
    /// </summary>
    public sealed class ScheduledNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduledNotificationService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        public ScheduledNotificationService(
            IServiceProvider serviceProvider,
            ILogger<ScheduledNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ScheduledNotificationService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledNotificationsAsync(stoppingToken);
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing scheduled notifications");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("ScheduledNotificationService stopped");
        }

        private async Task ProcessScheduledNotificationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationWriteRepository>();

            var currentTime = DateTime.UtcNow;

            // Get scheduled notifications that are due
            var notifications = await notificationRepository.GetScheduledNotificationsToSendAsync(
                currentTime,
                100,
                cancellationToken);

            if (!notifications.Any())
                return;

            _logger.LogInformation(
                "Processing {Count} scheduled notifications due at {CurrentTime}",
                notifications.Count,
                currentTime);

            foreach (var notification in notifications)
            {
                try
                {
                    // Queue the notification for sending
                    notification.Queue();
                    await notificationRepository.UpdateNotificationAsync(notification, cancellationToken);

                    _logger.LogInformation(
                        "Queued scheduled notification {NotificationId} for user {UserId}",
                        notification.Id.Value,
                        notification.RecipientId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to queue scheduled notification {NotificationId}",
                        notification.Id.Value);

                    notification.MarkAsFailed($"Failed to queue: {ex.Message}");
                    await notificationRepository.UpdateNotificationAsync(notification, cancellationToken);
                }
            }
        }
    }
}
