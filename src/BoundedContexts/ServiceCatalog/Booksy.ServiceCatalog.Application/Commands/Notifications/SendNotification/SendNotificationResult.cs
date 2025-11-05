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
        NotificationStatus Status,
        DateTime CreatedAt,
        string? GatewayMessageId = null,
        string? ErrorMessage = null);
}
