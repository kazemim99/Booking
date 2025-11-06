// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/ResendNotification/ResendNotificationCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.ResendNotification
{
    /// <summary>
    /// Handler for ResendNotificationCommand
    /// </summary>
    public sealed class ResendNotificationCommandHandler
        : ICommandHandler<ResendNotificationCommand, ResendNotificationResult>
    {
        private readonly INotificationWriteRepository _notificationRepository;
        private readonly IEmailNotificationService _emailService;
        private readonly ISmsNotificationService _smsService;
        private readonly IPushNotificationService _pushService;
        private readonly IInAppNotificationService _inAppService;
        private readonly ILogger<ResendNotificationCommandHandler> _logger;

        public ResendNotificationCommandHandler(
            INotificationWriteRepository notificationRepository,
            IEmailNotificationService emailService,
            ISmsNotificationService smsService,
            IPushNotificationService pushService,
            IInAppNotificationService inAppService,
            ILogger<ResendNotificationCommandHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _emailService = emailService;
            _smsService = smsService;
            _pushService = pushService;
            _inAppService = inAppService;
            _logger = logger;
        }

        public async Task<ResendNotificationResult> Handle(
            ResendNotificationCommand command,
            CancellationToken cancellationToken)
        {
            // Get notification
            var notificationId = NotificationId.From(command.NotificationId);
            var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);

            if (notification == null)
            {
                throw new NotFoundException($"Notification with ID {command.NotificationId} not found");
            }

            // Check if notification can be retried
            if (!notification.ShouldRetry())
            {
                throw new DomainValidationException(
                    nameof(command.NotificationId),
                    "Notification cannot be retried. Either it's not in Failed status or max retry attempts reached.");
            }

            // Check if expired
            if (notification.IsExpired())
            {
                throw new DomainValidationException(
                    nameof(command.NotificationId),
                    "Notification has expired and cannot be resent");
            }

            // Resend the notification
            try
            {
                await SendNotificationAsync(notification, cancellationToken);
                await _notificationRepository.UpdateNotificationAsync(notification, cancellationToken);

                _logger.LogInformation(
                    "Notification resent: NotificationId={NotificationId}, AttemptCount={AttemptCount}, Status={Status}",
                    notification.Id.Value,
                    notification.AttemptCount,
                    notification.Status);

                return new ResendNotificationResult(
                    notification.Id.Value,
                    notification.Status,
                    notification.AttemptCount,
                    notification.GatewayMessageId,
                    notification.ErrorMessage);
            }
            catch (Exception ex)
            {
                notification.MarkAsFailed(ex.Message);
                await _notificationRepository.UpdateNotificationAsync(notification, cancellationToken);

                _logger.LogWarning(ex,
                    "Failed to resend notification: NotificationId={NotificationId}",
                    notification.Id.Value);

                return new ResendNotificationResult(
                    notification.Id.Value,
                    notification.Status,
                    notification.AttemptCount,
                    null,
                    ex.Message);
            }
        }

        private async Task SendNotificationAsync(Domain.Aggregates.NotificationAggregate.Notification notification, CancellationToken cancellationToken)
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
