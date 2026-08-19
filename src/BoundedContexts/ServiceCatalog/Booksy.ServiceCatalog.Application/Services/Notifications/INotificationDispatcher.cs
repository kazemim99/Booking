// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/INotificationDispatcher.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;

namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Result of dispatching one notification across all the channels it names.
    /// </summary>
    /// <param name="Delivered">Channels the gateway accepted.</param>
    /// <param name="Skipped">Channels skipped by preferences, de-duplication, or a missing address.</param>
    /// <param name="Failed">Channels whose send failed and may be retried.</param>
    /// <param name="GatewayMessageId">Message id of the last successful send, for support lookups.</param>
    /// <param name="FirstError">First failure message, used as the notification's error text.</param>
    public sealed record NotificationDispatchOutcome(
        int Delivered,
        int Skipped,
        int Failed,
        string? GatewayMessageId,
        string? FirstError)
    {
        public bool AnyDelivered => Delivered > 0;

        public bool AnyFailed => Failed > 0;

        /// <summary>Nothing was sent and nothing failed — every channel was gated or already delivered.</summary>
        public bool EntirelySkipped => Delivered == 0 && Failed == 0;
    }

    /// <summary>
    /// The single send path for notifications: preference gate, de-duplication, per-channel send, delivery log.
    /// </summary>
    /// <remarks>
    /// Every caller that used to fan out to the channel services by hand (the send command, the queued-notification
    /// sweep, the scheduled-notification job, resend) goes through this instead, so the reliability rules cannot be
    /// enforced in one path and forgotten in another.
    /// </remarks>
    public interface INotificationDispatcher
    {
        /// <summary>
        /// Sends <paramref name="notification"/> and moves it to its resulting state (Sent/Delivered, Failed, or
        /// dead-lettered once the retry budget is spent). The caller is responsible for persisting it.
        /// </summary>
        Task<NotificationDispatchOutcome> DispatchAsync(
            Notification notification,
            CancellationToken cancellationToken = default);
    }
}
