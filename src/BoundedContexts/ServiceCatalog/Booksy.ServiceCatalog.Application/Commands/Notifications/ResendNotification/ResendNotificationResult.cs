// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/ResendNotification/ResendNotificationResult.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.ResendNotification
{
    /// <summary>
    /// Result of resending a notification
    /// </summary>
    public sealed record ResendNotificationResult(
        Guid NotificationId,
        NotificationStatus Status,
        int AttemptCount,
        string? GatewayMessageId = null,
        string? ErrorMessage = null)
    {
        /// <summary>
        /// Whether the resend delivered. Derived from the status the dispatcher left behind rather
        /// than assigned: as a settable property it was never set by the handler, so every
        /// successful resend reported <c>success: false</c> to the client.
        /// </summary>
        public bool Success => Status is NotificationStatus.Sent
            or NotificationStatus.Delivered
            or NotificationStatus.Read;
    }
}
