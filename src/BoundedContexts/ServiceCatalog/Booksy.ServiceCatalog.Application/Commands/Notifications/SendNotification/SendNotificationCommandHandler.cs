// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/SendNotification/SendNotificationCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Application.IntegrationEvents;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.SendNotification
{
    /// <summary>
    /// Handler for SendNotificationCommand
    /// </summary>
    public sealed class SendNotificationCommandHandler : ICommandHandler<SendNotificationCommand, SendNotificationResult>
    {
        private readonly INotificationWriteRepository _notificationRepository;
        private readonly INotificationDispatcher _dispatcher;
        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly ILogger<SendNotificationCommandHandler> _logger;

        public SendNotificationCommandHandler(
            INotificationWriteRepository notificationRepository,
            INotificationDispatcher dispatcher,
            IIntegrationEventPublisher eventPublisher,
            ILogger<SendNotificationCommandHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _dispatcher = dispatcher;
            _eventPublisher = eventPublisher;
            _logger = logger;
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

            // The originating event, when the caller knows it. It fixes the de-duplication scope, so a lifecycle
            // event delivered twice produces at most one notification per channel instead of two.
            if (command.IdempotencyKey is { } sourceEventId && sourceEventId != Guid.Empty)
            {
                notification.SetSourceEvent(sourceEventId);
            }

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

            // Queue the notification. Persisting before sending is what makes the send recoverable: if the process
            // dies mid-dispatch the row is still there for the retry sweep to pick up.
            notification.Queue();
            await _notificationRepository.SaveNotificationAsync(notification, cancellationToken);

            // One dispatcher owns the preference gate, de-duplication and per-channel fan-out. It reports failures
            // rather than throwing, so a dead gateway can no longer take the caller's request down with it.
            var outcome = await _dispatcher.DispatchAsync(notification, cancellationToken);
            await _notificationRepository.UpdateNotificationAsync(notification, cancellationToken);

            if (outcome.AnyFailed && notification.Status == NotificationStatus.Failed)
            {
                await RequestDurableRetryAsync(notification, outcome.FirstError, cancellationToken);
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

        /// <summary>
        /// Hands a failed-but-retryable notification to the CAP outbox. The subscriber retries it out of band, so a
        /// flaky gateway no longer costs the customer their notification just because the request ended.
        /// </summary>
        private async Task RequestDurableRetryAsync(
            Notification notification,
            string? error,
            CancellationToken cancellationToken)
        {
            try
            {
                await _eventPublisher.PublishAsync(
                    new NotificationRetryRequestedIntegrationEvent(
                        notification.Id.Value,
                        error ?? "Delivery failed"),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // The notification is already persisted as Failed, so the background sweep remains a backstop —
                // an unavailable bus must never turn a delivery problem into a failed API call.
                _logger.LogError(ex,
                    "Could not queue retry for notification {NotificationId}; the background sweep will retry it",
                    notification.Id.Value);
            }
        }
    }
}
