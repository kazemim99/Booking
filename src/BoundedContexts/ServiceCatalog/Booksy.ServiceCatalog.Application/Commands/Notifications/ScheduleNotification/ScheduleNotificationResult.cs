// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/ScheduleNotification/ScheduleNotificationResult.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.ScheduleNotification
{
    /// <summary>
    /// Result of scheduling a notification
    /// </summary>
    public sealed record ScheduleNotificationResult(
        Guid NotificationId,
        NotificationStatus Status,
        DateTime CreatedAt,
        DateTime ScheduledFor);
}
