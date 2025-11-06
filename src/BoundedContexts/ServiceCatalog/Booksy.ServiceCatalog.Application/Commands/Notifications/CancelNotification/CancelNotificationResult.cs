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
        string? CancellationReason);
}
