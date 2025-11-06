// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/CancelNotification/CancelNotificationCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.CancelNotification
{
    /// <summary>
    /// Handler for CancelNotificationCommand
    /// </summary>
    public sealed class CancelNotificationCommandHandler
        : ICommandHandler<CancelNotificationCommand, CancelNotificationResult>
    {
        private readonly INotificationWriteRepository _notificationRepository;
        private readonly ILogger<CancelNotificationCommandHandler> _logger;

        public CancelNotificationCommandHandler(
            INotificationWriteRepository notificationRepository,
            ILogger<CancelNotificationCommandHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<CancelNotificationResult> Handle(
            CancelNotificationCommand command,
            CancellationToken cancellationToken)
        {
            // Get notification
            var notificationId = NotificationId.From(command.NotificationId);
            var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);

            if (notification == null)
            {
                throw new NotFoundException($"Notification with ID {command.NotificationId} not found");
            }

            var previousStatus = notification.Status;

            // Cancel the notification (throws InvalidOperationException if cannot cancel)
            notification.Cancel(command.Reason);

            // Update in database
            await _notificationRepository.UpdateNotificationAsync(notification, cancellationToken);

            _logger.LogInformation(
                "Notification cancelled: NotificationId={NotificationId}, PreviousStatus={PreviousStatus}, Reason={Reason}",
                notification.Id.Value,
                previousStatus,
                command.Reason ?? "No reason provided");

            return new CancelNotificationResult(
                notification.Id.Value,
                previousStatus,
                notification.Status,
                command.Reason);
        }
    }
}
