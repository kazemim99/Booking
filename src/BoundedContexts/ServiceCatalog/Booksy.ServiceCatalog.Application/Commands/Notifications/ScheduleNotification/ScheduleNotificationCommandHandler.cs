// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/ScheduleNotification/ScheduleNotificationCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.ScheduleNotification
{
    /// <summary>
    /// Handler for ScheduleNotificationCommand
    /// </summary>
    public sealed class ScheduleNotificationCommandHandler
        : ICommandHandler<ScheduleNotificationCommand, ScheduleNotificationResult>
    {
        private readonly INotificationWriteRepository _notificationRepository;
        private readonly ILogger<ScheduleNotificationCommandHandler> _logger;

        public ScheduleNotificationCommandHandler(
            INotificationWriteRepository notificationRepository,
            ILogger<ScheduleNotificationCommandHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<ScheduleNotificationResult> Handle(
            ScheduleNotificationCommand command,
            CancellationToken cancellationToken)
        {
            // Validate scheduled time is in the future
            if (command.ScheduledFor <= DateTime.UtcNow)
            {
                throw new DomainValidationException(
                    nameof(command.ScheduledFor),
                    "Scheduled time must be in the future");
            }

            // Validate not too far in the future (e.g., max 90 days)
            if (command.ScheduledFor > DateTime.UtcNow.AddDays(90))
            {
                throw new DomainValidationException(
                    nameof(command.ScheduledFor),
                    "Scheduled time cannot be more than 90 days in the future");
            }

            // Create notification aggregate with scheduled time
            var notification = Notification.Create(
                UserId.From(command.RecipientId),
                command.Type,
                command.Channel,
                command.Subject,
                command.Body,
                command.Priority,
                command.PlainTextBody,
                scheduledFor: command.ScheduledFor);

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

            // Queue the notification (will be sent by background job at scheduled time)
            notification.Queue();

            // Save to database
            await _notificationRepository.SaveNotificationAsync(notification, cancellationToken);

            _logger.LogInformation(
                "Notification scheduled: NotificationId={NotificationId}, ScheduledFor={ScheduledFor}, Type={Type}, Channel={Channel}",
                notification.Id.Value,
                command.ScheduledFor,
                command.Type,
                command.Channel);

            return new ScheduleNotificationResult(
                notification.Id.Value,
                notification.Status,
                notification.CreatedAt,
                command.ScheduledFor);
        }
    }
}
