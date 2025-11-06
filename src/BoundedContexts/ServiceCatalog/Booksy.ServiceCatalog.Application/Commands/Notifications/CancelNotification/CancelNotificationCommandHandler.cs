// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/CancelNotification/CancelNotificationCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Results;
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

        public async Task<Result<CancelNotificationResult>> Handle(
            CancelNotificationCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get notification
                var notificationId = NotificationId.From(command.NotificationId);
                var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);

                if (notification == null)
                {
                    return Result<CancelNotificationResult>.Failure(
                        $"Notification not found: {command.NotificationId}");
                }

                var previousStatus = notification.Status;

                // Cancel the notification
                try
                {
                    notification.Cancel(command.Reason);
                }
                catch (InvalidOperationException ex)
                {
                    return Result<CancelNotificationResult>.Failure(ex.Message);
                }

                // Update in database
                await _notificationRepository.UpdateNotificationAsync(notification, cancellationToken);

                _logger.LogInformation(
                    "Notification cancelled: NotificationId={NotificationId}, PreviousStatus={PreviousStatus}, Reason={Reason}",
                    notification.Id.Value,
                    previousStatus,
                    command.Reason ?? "No reason provided");

                return Result<CancelNotificationResult>.Success(new CancelNotificationResult(
                    notification.Id.Value,
                    previousStatus,
                    notification.Status,
                    command.Reason));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel notification: {NotificationId}", command.NotificationId);
                return Result<CancelNotificationResult>.Failure($"Failed to cancel notification: {ex.Message}");
            }
        }
    }
}
