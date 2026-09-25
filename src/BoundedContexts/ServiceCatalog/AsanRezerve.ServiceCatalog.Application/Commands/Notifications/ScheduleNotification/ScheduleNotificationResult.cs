// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Notifications/ScheduleNotification/ScheduleNotificationResult.cs
// ========================================
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Notifications.ScheduleNotification
{
    /// <summary>
    /// Result of scheduling a notification
    /// </summary>
    public sealed record ScheduleNotificationResult(
        Guid NotificationId,
        NotificationStatus Status,
        DateTime CreatedAt,
        DateTime ScheduledFor)
    {
        /// <summary>
        /// Whether the notification is scheduled. Derived from the aggregate's state rather than
        /// assigned: as a settable property it was never set by the handler, so every successful
        /// schedule reported <c>success: false</c> to the client.
        /// </summary>
        public bool Success => Status == NotificationStatus.Queued;
    }
}
