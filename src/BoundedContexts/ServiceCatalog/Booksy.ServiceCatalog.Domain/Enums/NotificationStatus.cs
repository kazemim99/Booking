// ========================================
// Booksy.ServiceCatalog.Domain/Enums/NotificationStatus.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Represents the lifecycle status of a notification
    /// </summary>
    public enum NotificationStatus
    {
        /// <summary>
        /// Notification created but not yet processed
        /// </summary>
        Pending,

        /// <summary>
        /// Notification queued for delivery
        /// </summary>
        Queued,

        /// <summary>
        /// Notification sent to delivery service
        /// </summary>
        Sent,

        /// <summary>
        /// Notification successfully delivered to recipient
        /// </summary>
        Delivered,

        /// <summary>
        /// Notification opened/read by recipient (email, in-app)
        /// </summary>
        Read,

        /// <summary>
        /// Notification failed to deliver
        /// </summary>
        Failed,

        /// <summary>
        /// Notification bounced (email)
        /// </summary>
        Bounced,

        /// <summary>
        /// Notification cancelled before delivery
        /// </summary>
        Cancelled,

        /// <summary>
        /// Notification expired (scheduled notification not sent)
        /// </summary>
        Expired
    }
}
