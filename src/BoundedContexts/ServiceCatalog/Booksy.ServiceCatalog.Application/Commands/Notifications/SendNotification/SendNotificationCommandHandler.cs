// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/SendNotification/SendNotificationCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.SendNotification
{
    /// <summary>
    /// Handler for SendNotificationCommand
    /// </summary>
    public sealed class SendNotificationCommandHandler : ICommandHandler<SendNotificationCommand, SendNotificationResult>
    {
        private readonly INotificationWriteRepository _notificationRepository;
        private readonly IEmailNotificationService _emailService;
        private readonly ISmsNotificationService _smsService;
        private readonly IPushNotificationService _pushService;
        private readonly IInAppNotificationService _inAppService;

        public SendNotificationCommandHandler(
            INotificationWriteRepository notificationRepository,
            IEmailNotificationService emailService,
            ISmsNotificationService smsService,
            IPushNotificationService pushService,
            IInAppNotificationService inAppService)
        {
            _notificationRepository = notificationRepository;
            _emailService = emailService;
            _smsService = smsService;
            _pushService = pushService;
            _inAppService = inAppService;
        }

        public async Task<SendNotificationResult> Handle(
            SendNotificationCommand command,
            CancellationToken cancellationToken)
        {
            // Create notification aggregate
            var notification = Notification.Create(
                UserId.From(command.RecipientId),
                command.Type,
                command.Channel,
                command.Subject,
                command.Body,
                command.Priority,
                command.PlainTextBody);

            // Set recipient contact information
            notification.SetRecipientContact(
                command.RecipientEmail,
                command.RecipientPhone,
                command.RecipientName);

            // Set template information if provided
            if (!string.IsNullOrWhiteSpace(command.TemplateId) && command.TemplateData != null)
            {
                notification.SetTemplate(command.TemplateId, command.TemplateData);
            }

            // Set related entities
            notification.SetRelatedEntities(
                command.BookingId.HasValue ? BookingId.From(command.BookingId.Value) : null,
                command.PaymentId.HasValue ? PaymentId.From(command.PaymentId.Value) : null,
                command.ProviderId.HasValue ? ProviderId.From(command.ProviderId.Value) : null);

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

            // Send immediately based on channel
            try
            {
                await SendNotificationAsync(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                // Mark as failed but don't throw - notification is saved and can be retried
                notification.MarkAsFailed(ex.Message);
                await _notificationRepository.UpdateNotificationAsync(notification, cancellationToken);
            }

            return new SendNotificationResult(
                notification.Id.Value,
                notification.Status == NotificationStatus.Delivered || notification.Status == NotificationStatus.Sent,
                notification.Channel,
                notification.Status,
                notification.CreatedAt,
                notification.SentAt,
                notification.GatewayMessageId,
                notification.ErrorMessage);
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
                    // For push notifications, we'd need device token from user preferences
                    // For now, mark as sent
                    var pushResult = await _pushService.SendPushAsync(
                        "device-token-placeholder", // TODO: Get from user preferences
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
