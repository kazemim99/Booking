// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/SendNotification/SendNotificationResult.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.SendNotification
{
    /// <summary>
    /// Result of sending a notification
    /// </summary>
    public sealed record SendNotificationResult(
        Guid NotificationId,
        bool Success,
        NotificationChannel Channel,
        NotificationStatus Status,
        DateTime CreatedAt,
        DateTime? SentAt = null,
        string? GatewayMessageId = null,
        string? ErrorMessage = null);
}
