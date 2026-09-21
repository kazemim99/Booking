using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Records the intent to notify somebody. The only supported way for business code to cause a notification.
    /// </summary>
    /// <remarks>
    /// <para>Calling this does not send anything. It writes a row on the caller's unit of work, so the intent
    /// commits with the business change or disappears with it. A sweep picks it up afterwards and sends it
    /// through the dispatcher, which applies preferences, de-duplication and retry as it always has.</para>
    ///
    /// <para><b>What the caller does not decide.</b> Channels, criticality and whether the recipient may
    /// suppress it all come from the notification catalogue, keyed by <see cref="NotificationEventCode"/>.
    /// A caller cannot ask for an SMS: it names the notification, and the catalogue says what that is. This is
    /// what keeps the product's notification set reviewable in one place rather than spread across handlers.</para>
    ///
    /// <para><b>Why the caller must supply the parameters.</b> The sweep runs later and must not re-load the
    /// subject to build the text — the subject may have changed or been deleted by then, and re-reading is
    /// exactly the coupling that makes a notification quietly wrong. Everything the copy needs is captured
    /// here, at the moment the thing actually happened.</para>
    /// </remarks>
    public interface INotificationRaiser
    {
        /// <summary>
        /// Records one notification intent.
        /// </summary>
        /// <param name="code">Which notification. Must have a catalogue entry.</param>
        /// <param name="recipientId">Who is being told.</param>
        /// <param name="dedupKey">
        /// Scopes de-duplication. Use the id of the thing that happened — a domain event's id, or the
        /// booking's id for a reminder — so that a redelivered event or a retried command records the intent
        /// once. The same key with a different recipient is a different intent, so telling both the customer
        /// and the salon about one event is two calls with one key.
        /// </param>
        /// <param name="parameters">Template parameters, captured now.</param>
        /// <param name="subjectType">What it is about ("Booking", "Payment", "Provider"), for the inbox link.</param>
        /// <param name="subjectId">Id of that subject.</param>
        /// <param name="scheduledFor">
        /// When it should go out. Null means as soon as the next sweep runs; a future time is how a reminder
        /// is scheduled.
        /// </param>
        Task RaiseAsync(
            NotificationEventCode code,
            Guid recipientId,
            Guid dedupKey,
            IReadOnlyDictionary<string, string>? parameters = null,
            string? subjectType = null,
            Guid? subjectId = null,
            DateTime? scheduledFor = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Withdraws anything not yet sent about a subject — the reminders of a booking that is no longer
        /// happening. Notifications already sent are not affected; they cannot be recalled.
        /// </summary>
        /// <param name="onlyCodes">
        /// Limits the withdrawal to these notifications. Omit it when the subject itself is off, so that
        /// nothing about it goes out; name the codes when only one conversation about a subject has ended.
        /// A review, for instance, ends the asking and says nothing about a pending refund notice filed
        /// under the same booking.
        /// </param>
        Task<int> WithdrawPendingForSubjectAsync(
            string subjectType,
            Guid subjectId,
            IReadOnlyCollection<NotificationEventCode>? onlyCodes = null,
            CancellationToken cancellationToken = default);
    }
}
