// ========================================
// Booksy.ServiceCatalog.Application/Services/BackgroundServices/NotificationProcessorService.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Services.BackgroundServices
{
    /// <summary>
    /// Background service that processes queued notifications
    /// </summary>
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
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
            var smsService = scope.ServiceProvider.GetRequiredService<ISmsNotificationService>();
            var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
            var inAppService = scope.ServiceProvider.GetRequiredService<IInAppNotificationService>();

            // Get pending notifications (queued or failed with retry)
            var notifications = await notificationRepository.GetPendingNotificationsAsync(100, cancellationToken);

            if (!notifications.Any())
                return;

            _logger.LogInformation("Processing {Count} queued notifications", notifications.Count);

            foreach (var notification in notifications)
            {
                try
                {
                    // Send via appropriate channels
                    if (notification.Channel.HasFlag(NotificationChannel.Email))
                    {
                        await emailService.SendEmailAsync(
                            notification.RecipientEmail ?? "",
                            notification.Subject,
                            notification.Body,
                            notification.PlainTextBody,
                            cancellationToken);
                    }

                    if (notification.Channel.HasFlag(NotificationChannel.SMS))
                    {
                        await smsService.SendSmsAsync(
                            notification.RecipientPhone ?? "",
                            notification.PlainTextBody ?? notification.Body,
                            cancellationToken);
                    }

                    if (notification.Channel.HasFlag(NotificationChannel.PushNotification))
                    {
                        await pushService.SendPushAsync(
                            notification.RecipientId.Value,
                            notification.Subject,
                            notification.PlainTextBody ?? notification.Body,
                            notification.Metadata,
                            cancellationToken);
                    }

                    if (notification.Channel.HasFlag(NotificationChannel.InApp))
                    {
                        await inAppService.SendToUserAsync(
                            notification.RecipientId.Value,
                            notification.Subject,
                            notification.Body,
                            notification.Type.ToString(),
                            notification.Metadata,
                            cancellationToken);
                    }

                    // Mark as sent
                    notification.MarkAsSent(Guid.NewGuid().ToString());
                    await notificationRepository.UpdateNotificationAsync(notification, cancellationToken);

                    _logger.LogInformation(
                        "Sent notification {NotificationId} via {Channels} to user {UserId}",
                        notification.Id.Value,
                        notification.Channel,
                        notification.RecipientId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send notification {NotificationId}",
                        notification.Id.Value);

                    notification.MarkAsFailed(ex.Message);
                    await notificationRepository.UpdateNotificationAsync(notification, cancellationToken);
                }
            }
        }
    }
}
