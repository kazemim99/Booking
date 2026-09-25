using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Notifications
{
    /// <summary>
    /// One recorded intent to notify somebody, written in the same transaction as the work that caused it.
    /// </summary>
    /// <remarks>
    /// <para>The row exists so that raising a notification and doing the business write succeed or fail
    /// together. A sweep picks pending rows up afterwards and turns each into a <c>Notification</c>, which the
    /// existing dispatcher sends. Nothing here sends anything.</para>
    ///
    /// <para><b>Why an intent and not a domain event handler.</b> ServiceCatalog dispatches domain events
    /// <i>before</i> <c>SaveChangesAsync</c> on the transactional path (FOLLOW-UPS #66), so a handler runs
    /// while the aggregate that raised the event is not yet in the database: it cannot re-read its own
    /// subject, and anything it sends has already escaped if the transaction then rolls back. A row written
    /// inside the transaction has neither problem — it disappears on rollback, and the sweep that reads it
    /// runs long after the commit.</para>
    ///
    /// <para>The row therefore carries everything the notification needs as data. A sweep must never have to
    /// re-derive content by loading the subject, or it inherits the same coupling in a slower form.</para>
    /// </remarks>
    public sealed class NotificationOutboxEntry
    {
        public Guid Id { get; set; }

        /// <summary>Which notification this is. The catalogue turns it into audience, channels and criticality.</summary>
        public NotificationEventCode EventCode { get; set; }

        /// <summary>Who is being notified — a customer, a provider owner, or a staff member.</summary>
        public Guid RecipientId { get; set; }

        /// <summary>What it is about ("Booking", "Payment", "Provider"), for the inbox's tap destination.</summary>
        public string? SubjectType { get; set; }

        public Guid? SubjectId { get; set; }

        /// <summary>
        /// The template parameters, as JSON. Everything the copy needs, captured at raise time.
        /// </summary>
        public string ParametersJson { get; set; } = "{}";

        /// <summary>When it becomes due. Null means immediately; a future value is how a reminder is scheduled.</summary>
        public DateTime? ScheduledFor { get; set; }

        public string State { get; set; } = NotificationOutboxState.Pending;

        /// <summary>
        /// Lease expiry while a sweep holds this row. A sweep that dies mid-flight leaves a lease that simply
        /// runs out, so the row returns to the pool without anyone having to detect the crash.
        /// </summary>
        public DateTime? ClaimedUntil { get; set; }

        public int AttemptCount { get; set; }

        public string? LastError { get; set; }

        /// <summary>
        /// Scopes de-duplication: the id of the event that caused this intent. Together with the event code
        /// and recipient it is unique, so redelivering the same event — or retrying the command — records one
        /// intent, not two. One event that notifies both the customer and the salon still produces two rows,
        /// because the recipient differs.
        /// </summary>
        public Guid DedupKey { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // The state names live on NotificationOutboxState in the domain, beside the policy that decides the
        // transitions. Duplicating them here would let the table and the rules drift apart silently.
    }
}
