// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/ResendNotification/ResendNotificationCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Application.Services.Notifications;
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
        private readonly INotificationDispatcher _dispatcher;
        private readonly ILogger<ResendNotificationCommandHandler> _logger;

        public ResendNotificationCommandHandler(
            INotificationWriteRepository notificationRepository,
            INotificationDispatcher dispatcher,
            ILogger<ResendNotificationCommandHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _dispatcher = dispatcher;
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

            // Resend through the shared dispatcher so a manual retry obeys the same preference gate and
            // de-duplication rules as an automatic one — a resend must not re-deliver a channel that already
            // succeeded on an earlier attempt.
            await _dispatcher.DispatchAsync(notification, cancellationToken);
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
    }
}
