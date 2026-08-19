// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/INotificationDeliveryLog.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Outcome of trying to claim a delivery slot for one (event, channel, recipient) tuple.
    /// </summary>
    public enum NotificationDeliveryClaim
    {
        /// <summary>The caller owns this delivery and must attempt the send.</summary>
        Claimed,

        /// <summary>This exact notification already reached this recipient on this channel — do not send again.</summary>
        AlreadyDelivered
    }

    /// <summary>
    /// Durable record of every delivery attempt, doubling as the de-duplication gate.
    /// </summary>
    /// <remarks>
    /// The transport guarantees at-least-once (an event can be re-dispatched after a crash, a retry, or a CAP
    /// redelivery). Claiming the tuple <c>(EventId, Channel, Recipient)</c> before sending turns that into
    /// effectively-once: the first caller to claim sends, and any later dispatch for the same tuple is told the
    /// message already went out. A previously *failed* tuple is re-claimable — a failure must stay retryable, or
    /// dedup would convert one transient gateway hiccup into a permanently missing notification.
    /// </remarks>
    public interface INotificationDeliveryLog
    {
        /// <summary>
        /// Atomically claims the delivery slot for this tuple.
        /// </summary>
        Task<NotificationDeliveryClaim> TryClaimAsync(
            Guid eventId,
            NotificationChannel channel,
            string recipient,
            Guid notificationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records the outcome of the attempt the caller claimed. A successful outcome closes the tuple for good.
        /// </summary>
        Task RecordOutcomeAsync(
            Guid eventId,
            NotificationChannel channel,
            string recipient,
            bool success,
            string? gatewayMessageId,
            string? errorMessage,
            CancellationToken cancellationToken = default);
    }
}
