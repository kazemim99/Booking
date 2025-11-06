// ========================================
// Booksy.ServiceCatalog.Infrastructure/BackgroundJobs/ProcessScheduledNotificationsJob.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Background job that processes scheduled notifications
    /// Runs periodically (every minute) to send notifications that are due
    /// </summary>
    public class ProcessScheduledNotificationsJob
    {
        private readonly INotificationReadRepository _readRepo;
        private readonly INotificationWriteRepository _writeRepo;
        private readonly IEmailNotificationService _emailService;
        private readonly ISmsNotificationService _smsService;
        private readonly IPushNotificationService _pushService;
        private readonly IInAppNotificationService _inAppService;
        private readonly ILogger<ProcessScheduledNotificationsJob> _logger;

        public ProcessScheduledNotificationsJob(
            INotificationReadRepository readRepo,
            INotificationWriteRepository writeRepo,
            IEmailNotificationService emailService,
            ISmsNotificationService smsService,
            IPushNotificationService pushService,
            IInAppNotificationService inAppService,
            ILogger<ProcessScheduledNotificationsJob> logger)
        {
            _readRepo = readRepo;
            _writeRepo = writeRepo;
            _emailService = emailService;
            _smsService = smsService;
            _pushService = pushService;
            _inAppService = inAppService;
            _logger = logger;
        }

        /// <summary>
        /// Executes the job to process scheduled notifications
        /// </summary>
        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Starting ProcessScheduledNotificationsJob execution");

                // Get notifications due now
                var notifications = await _readRepo.GetScheduledNotificationsDueAsync(ct);

                _logger.LogInformation(
                    "Found {Count} scheduled notifications due for processing",
                    notifications.Count);

                foreach (var notification in notifications)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Processing scheduled notification {NotificationId} for user {RecipientId}, Channel: {Channel}",
                            notification.Id.Value,
                            notification.RecipientId.Value,
                            notification.Channel);

                        // Send based on channel
                        var messageId = await SendNotificationAsync(notification, ct);

                        // Mark as delivered
                        notification.MarkAsDelivered(messageId);

                        _logger.LogInformation(
                            "Successfully sent scheduled notification {NotificationId}. MessageId: {MessageId}",
                            notification.Id.Value,
                            messageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to send scheduled notification {NotificationId} for user {RecipientId}",
                            notification.Id.Value,
                            notification.RecipientId.Value);

                        // Mark as failed
                        notification.MarkAsFailed(ex.Message);
                    }
                    finally
                    {
                        // Update notification status in database
                        await _writeRepo.UpdateNotificationAsync(notification, ct);
                    }
                }

                _logger.LogInformation(
                    "Completed ProcessScheduledNotificationsJob execution. Processed {Count} notifications",
                    notifications.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing ProcessScheduledNotificationsJob");
                throw;
            }
        }

        /// <summary>
        /// Sends notification based on its channel
        /// </summary>
        private async Task<string?> SendNotificationAsync(
            Domain.Aggregates.NotificationAggregate.Notification notification,
            CancellationToken ct)
        {
            return notification.Channel switch
            {
                NotificationChannel.Email => await SendEmailAsync(notification, ct),
                NotificationChannel.SMS => await SendSmsAsync(notification, ct),
                NotificationChannel.PushNotification => await SendPushAsync(notification, ct),
                NotificationChannel.InApp => await SendInAppAsync(notification, ct),
                _ => throw new NotSupportedException($"Notification channel {notification.Channel} is not supported")
            };
        }

        /// <summary>
        /// Sends email notification
        /// </summary>
        private async Task<string?> SendEmailAsync(
            Domain.Aggregates.NotificationAggregate.Notification notification,
            CancellationToken ct)
        {
            var (success, messageId, errorMessage) = await _emailService.SendEmailAsync(
                to: notification.RecipientEmail ?? throw new InvalidOperationException("RecipientEmail is required for email notifications"),
                subject: notification.Subject ?? "Notification",
                htmlBody: notification.Body,
                plainTextBody: notification.PlainTextBody ?? notification.Body,
                cancellationToken: ct);

            if (!success)
            {
                throw new InvalidOperationException($"Failed to send email: {errorMessage}");
            }

            return messageId;
        }

        /// <summary>
        /// Sends SMS notification
        /// </summary>
        private async Task<string?> SendSmsAsync(
            Domain.Aggregates.NotificationAggregate.Notification notification,
            CancellationToken ct)
        {
            var (success, messageId, errorMessage) = await _smsService.SendSmsAsync(
                phoneNumber: notification.RecipientPhone ?? throw new InvalidOperationException("RecipientPhone is required for SMS notifications"),
                message: notification.PlainTextBody ?? notification.Body,
                cancellationToken: ct);

            if (!success)
            {
                throw new InvalidOperationException($"Failed to send SMS: {errorMessage}");
            }

            return messageId;
        }

        /// <summary>
        /// Sends push notification
        /// </summary>
        private async Task<string?> SendPushAsync(
            Domain.Aggregates.NotificationAggregate.Notification notification,
            CancellationToken ct)
        {
            var metadata = notification.Metadata?.ToDictionary(k => k.Key, v => (object)v.Value)
                ?? new Dictionary<string, object>();

            var (success, messageId, errorMessage) = await _pushService.SendPushAsync(
                userId: notification.RecipientId.Value,
                title: notification.Subject ?? "Notification",
                body: notification.Body,
                data: metadata,
                cancellationToken: ct);

            if (!success)
            {
                throw new InvalidOperationException($"Failed to send push notification: {errorMessage}");
            }

            return messageId;
        }

        /// <summary>
        /// Sends in-app notification
        /// </summary>
        private async Task<string?> SendInAppAsync(
            Domain.Aggregates.NotificationAggregate.Notification notification,
            CancellationToken ct)
        {
            var metadata = notification.Metadata?.ToDictionary(k => k.Key, v => (object)v.Value)
                ?? new Dictionary<string, object>();

            var (success, errorMessage) = await _inAppService.SendToUserAsync(
                userId: notification.RecipientId.Value,
                title: notification.Subject ?? "Notification",
                message: notification.Body,
                type: notification.Type.ToString(),
                metadata: metadata,
                cancellationToken: ct);

            if (!success)
            {
                throw new InvalidOperationException($"Failed to send in-app notification: {errorMessage}");
            }

            // In-app notifications don't have external message IDs
            return notification.Id.Value.ToString();
        }
    }
}
