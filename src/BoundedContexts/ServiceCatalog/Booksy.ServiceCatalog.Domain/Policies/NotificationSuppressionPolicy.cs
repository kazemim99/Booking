// ========================================
// Booksy.ServiceCatalog.Domain/Policies/NotificationSuppressionPolicy.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Policies
{
    /// <summary>
    /// Decides whether a notification may be sent on a channel, given the recipient's preferences.
    /// </summary>
    /// <remarks>
    /// <para>Preferences are honoured for ordinary notifications, but a documented set is
    /// <b>non-suppressible</b>: money movement (payments, refunds, payouts, invoices) and account security
    /// (verification codes, password resets, security alerts). Those either carry a financial record the
    /// customer is entitled to, or are the only way to complete an action the customer just started — letting
    /// a stale "SMS off" toggle swallow a refund receipt or an OTP is worse than an unwanted message.</para>
    /// <para>The set is deliberately conservative: it can only ever cause a notification to be <i>sent</i>.
    /// Moving an entry out of it (making it suppressible) is the change that needs product/legal sign-off,
    /// not adding one.</para>
    /// </remarks>
    public static class NotificationSuppressionPolicy
    {
        /// <summary>
        /// Notification types that ignore channel preferences.
        /// </summary>
        /// <remarks>
        /// A <see cref="HashSet{T}"/> rather than a flags mask on purpose: <see cref="NotificationType"/> is
        /// declared <c>[Flags]</c> but its later members (16777216 and up) are sequential integers, not distinct
        /// bits, so <c>HasFlag</c> reports false positives between them (e.g. <c>RefundIssued</c> "contains"
        /// <c>RefundProcessed</c>). Set membership is the only correct test for those values.
        /// </remarks>
        public static readonly IReadOnlySet<NotificationType> NonSuppressible = new HashSet<NotificationType>
        {
            // Money movement — the customer is entitled to the record.
            NotificationType.PaymentReceived,
            NotificationType.PaymentFailed,
            NotificationType.PaymentRefunded,
            NotificationType.PaymentConfirmed,
            NotificationType.RefundProcessed,
            NotificationType.RefundIssued,
            NotificationType.PayoutCompleted,
            NotificationType.PayoutProcessed,
            NotificationType.InvoiceGenerated,

            // Account security — suppressing these strands the user mid-flow.
            NotificationType.SecurityAlert,
            NotificationType.PasswordReset,
            NotificationType.PhoneVerification,
            NotificationType.AccountVerification,
        };

        /// <summary>
        /// True when the recipient's preferences are allowed to stop this notification type.
        /// </summary>
        public static bool IsSuppressible(NotificationType type) => !NonSuppressible.Contains(type);

        /// <summary>
        /// Whether <paramref name="channel"/> may be used for <paramref name="type"/>.
        /// </summary>
        /// <param name="preferences">
        /// The recipient's stored preferences, or <c>null</c> when they have never set any — in which case the
        /// notification is sent. Absent preferences must never read as "everything disabled": that would
        /// silently mute every notification for every user who has not visited the settings screen.
        /// </param>
        public static bool ShouldSend(
            UserNotificationPreferences? preferences,
            NotificationChannel channel,
            NotificationType type)
        {
            if (!IsSuppressible(type))
                return true;

            if (preferences is null)
                return true;

            return preferences.Preferences.IsChannelEnabled(channel);
        }
    }
}
