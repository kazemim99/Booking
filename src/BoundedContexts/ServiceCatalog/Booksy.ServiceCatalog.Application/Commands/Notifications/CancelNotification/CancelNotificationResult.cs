// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/CancelNotification/CancelNotificationResult.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.CancelNotification
{
    /// <summary>
    /// Result of canceling a notification
    /// </summary>
    public sealed record CancelNotificationResult(
        Guid NotificationId,
        NotificationStatus PreviousStatus,
        NotificationStatus CurrentStatus,
        string? CancellationReason)
    {
        /// <summary>
        /// Whether the notification is now cancelled. Derived from the aggregate's state rather
        /// than assigned: as a settable property it was never set by the handler, so every
        /// successful cancellation reported <c>success: false</c> to the client.
        /// </summary>
        public bool Success => CurrentStatus == NotificationStatus.Cancelled;

        public string Message => Success
            ? $"Notification {NotificationId} was cancelled"
            : $"Notification {NotificationId} could not be cancelled from {PreviousStatus} status";
    }
}
