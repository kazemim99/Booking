// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/SendBulkNotification/SendBulkNotificationCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Results;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.SendBulkNotification
{
    /// <summary>
    /// Handler for SendBulkNotificationCommand
    /// </summary>
    public sealed class SendBulkNotificationCommandHandler
        : ICommandHandler<SendBulkNotificationCommand, SendBulkNotificationResult>
    {
        private readonly INotificationWriteRepository _notificationRepository;
        private readonly IEmailNotificationService _emailService;
        private readonly ISmsNotificationService _smsService;
        private readonly IPushNotificationService _pushService;
        private readonly IInAppNotificationService _inAppService;
        private readonly ILogger<SendBulkNotificationCommandHandler> _logger;

        public SendBulkNotificationCommandHandler(
            INotificationWriteRepository notificationRepository,
            IEmailNotificationService emailService,
            ISmsNotificationService smsService,
            IPushNotificationService pushService,
            IInAppNotificationService inAppService,
            ILogger<SendBulkNotificationCommandHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _emailService = emailService;
            _smsService = smsService;
            _pushService = pushService;
            _inAppService = inAppService;
            _logger = logger;
        }

        public async Task<Result<SendBulkNotificationResult>> Handle(
            SendBulkNotificationCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                // Validate recipients
                if (command.RecipientIds == null || !command.RecipientIds.Any())
                {
                    return Result<SendBulkNotificationResult>.Failure("No recipients provided");
                }

                // Limit batch size
                if (command.RecipientIds.Count > 1000)
                {
                    return Result<SendBulkNotificationResult>.Failure(
                        "Maximum 1000 recipients per batch. Please split into multiple batches.");
                }

                // Generate unique batch ID
                var batchId = Guid.NewGuid().ToString();

                var notificationIds = new List<Guid>();
                var errors = new List<string>();
                var successCount = 0;
                var failureCount = 0;

                // Process each recipient
                foreach (var recipientId in command.RecipientIds)
                {
                    try
                    {
                        // Create notification for this recipient
                        var notification = Notification.Create(
                            UserId.From(recipientId),
                            command.Type,
                            command.Channel,
                            command.Subject,
                            command.Body,
                            command.Priority,
                            command.PlainTextBody);

                        // Set template if provided
                        if (!string.IsNullOrWhiteSpace(command.TemplateId) && command.TemplateData != null)
                        {
                            notification.SetTemplate(command.TemplateId, command.TemplateData);
                        }

                        // Set campaign and batch ID
                        notification.SetCampaign(command.CampaignId, batchId);

                        // Add metadata
                        if (command.Metadata != null)
                        {
                            foreach (var kvp in command.Metadata)
                            {
                                notification.AddMetadata(kvp.Key, kvp.Value);
                            }
                        }

                        // Queue the notification
                        notification.Queue();

                        // Save to database
                        await _notificationRepository.SaveNotificationAsync(notification, cancellationToken);

                        notificationIds.Add(notification.Id.Value);

                        // Try to send immediately (don't wait for all to complete)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await SendNotificationAsync(notification, cancellationToken);
                                await _notificationRepository.UpdateNotificationAsync(notification, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex,
                                    "Failed to send notification in bulk: NotificationId={NotificationId}",
                                    notification.Id.Value);
                                notification.MarkAsFailed(ex.Message);
                                await _notificationRepository.UpdateNotificationAsync(notification, cancellationToken);
                            }
                        }, cancellationToken);

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to create notification for recipient: RecipientId={RecipientId}",
                            recipientId);
                        errors.Add($"Recipient {recipientId}: {ex.Message}");
                        failureCount++;
                    }
                }

                _logger.LogInformation(
                    "Bulk notification initiated: BatchId={BatchId}, Total={Total}, Success={Success}, Failure={Failure}",
                    batchId,
                    command.RecipientIds.Count,
                    successCount,
                    failureCount);

                return Result<SendBulkNotificationResult>.Success(new SendBulkNotificationResult(
                    batchId,
                    command.RecipientIds.Count,
                    successCount,
                    failureCount,
                    notificationIds,
                    errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk notification");
                return Result<SendBulkNotificationResult>.Failure($"Failed to send bulk notification: {ex.Message}");
            }
        }

        private async Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
        {
            bool success;
            string? messageId = null;
            string? errorMessage = null;

            switch (notification.Channel)
            {
                case NotificationChannel.Email:
                    if (string.IsNullOrWhiteSpace(notification.RecipientEmail))
                    {
                        throw new InvalidOperationException("Recipient email is required for email notifications");
                    }

                    var emailResult = await _emailService.SendEmailAsync(
                        notification.RecipientEmail,
                        notification.Subject,
                        notification.Body,
                        notification.PlainTextBody,
                        metadata: notification.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        cancellationToken: cancellationToken);

                    success = emailResult.Success;
                    messageId = emailResult.MessageId;
                    errorMessage = emailResult.ErrorMessage;
                    break;

                case NotificationChannel.SMS:
                    if (string.IsNullOrWhiteSpace(notification.RecipientPhone))
                    {
                        throw new InvalidOperationException("Recipient phone is required for SMS notifications");
                    }

                    var smsResult = await _smsService.SendSmsAsync(
                        notification.RecipientPhone,
                        notification.Body,
                        notification.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        cancellationToken);

                    success = smsResult.Success;
                    messageId = smsResult.MessageId;
                    errorMessage = smsResult.ErrorMessage;
                    break;

                case NotificationChannel.PushNotification:
                    var pushResult = await _pushService.SendPushAsync(
                        "device-token-placeholder",
                        notification.Subject,
                        notification.Body,
                        notification.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        cancellationToken);

                    success = pushResult.Success;
                    messageId = pushResult.MessageId;
                    errorMessage = pushResult.ErrorMessage;
                    break;

                case NotificationChannel.InApp:
                    var inAppResult = await _inAppService.SendToUserAsync(
                        notification.RecipientId.Value,
                        notification.Subject,
                        notification.Body,
                        notification.Type.ToString(),
                        notification.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        cancellationToken);

                    success = inAppResult.Success;
                    errorMessage = inAppResult.ErrorMessage;
                    break;

                default:
                    throw new NotSupportedException($"Notification channel {notification.Channel} is not supported");
            }

            // Update notification status
            notification.Send();

            if (success)
            {
                notification.MarkAsDelivered(messageId);
            }
            else
            {
                notification.MarkAsFailed(errorMessage ?? "Unknown error");
            }
        }
    }
}
