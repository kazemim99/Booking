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
        string? ErrorMessage = null);
}
