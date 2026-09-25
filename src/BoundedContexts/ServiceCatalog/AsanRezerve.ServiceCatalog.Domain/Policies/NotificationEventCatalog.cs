using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Domain.Policies
{
    /// <summary>Who a notification is addressed to.</summary>
    public enum NotificationAudience
    {
        Customer,
        Provider,
        StaffMember,
    }

    /// <summary>
    /// Whether the recipient is allowed to switch a notification off.
    /// </summary>
    /// <remarks>
    /// <b>Critical means "may not be suppressed", and nothing else.</b> It is not a styling axis and not a
    /// measure of how important something feels — it is a product decision that this notification is either
    /// the only way to finish something the person started, or a record they are entitled to. Keep the set
    /// small: every addition takes a choice away from the recipient.
    /// </remarks>
    public enum NotificationCriticality
    {
        /// <summary>Honours the recipient's channel preferences.</summary>
        Standard,

        /// <summary>Sent regardless of preferences.</summary>
        Critical,
    }

    /// <summary>What tapping a notification opens.</summary>
    public enum NotificationDestinationKind
    {
        None,
        Booking,
        Payment,
        Payout,
        Provider,
        Invitation,
        Profile,
    }

    /// <summary>What one notification is.</summary>
    /// <param name="Audience">Who receives it.</param>
    /// <param name="Channels">The channels it may use. The recipient's preferences narrow this further.</param>
    /// <param name="Criticality">Whether the recipient may switch it off.</param>
    /// <param name="Destination">What tapping it opens.</param>
    public sealed record NotificationDescriptor(
        NotificationAudience Audience,
        NotificationChannel Channels,
        NotificationCriticality Criticality,
        NotificationDestinationKind Destination)
    {
        /// <summary>Derived, never stored separately: suppressibility is exactly the inverse of criticality.</summary>
        public bool IsSuppressible => Criticality == NotificationCriticality.Standard;
    }

    /// <summary>
    /// Every notification the product can send, and what each one is.
    /// </summary>
    /// <remarks>
    /// <para>One table so that the product's notification set can be reviewed by reading a single file rather
    /// than grepping handlers. A notification cannot be raised without an entry here, which means nobody can
    /// add one without stating who receives it, on what, and whether they can turn it off.</para>
    ///
    /// <para><b>SMS is reserved for critical notifications</b> (user decision, 2026-09-19). Each message costs
    /// money, so a non-critical entry naming SMS is a defect and the self-validation test fails on it.</para>
    /// </remarks>
    public static class NotificationEventCatalog
    {
        private const NotificationChannel PushInApp = NotificationChannel.PushNotification | NotificationChannel.InApp;
        private const NotificationChannel SmsPushInApp = NotificationChannel.SMS | PushInApp;
        private const NotificationChannel SmsPush = NotificationChannel.SMS | NotificationChannel.PushNotification;

        private const NotificationCriticality Standard = NotificationCriticality.Standard;
        private const NotificationCriticality Critical = NotificationCriticality.Critical;

        private static readonly IReadOnlyDictionary<NotificationEventCode, NotificationDescriptor> Entries =
            new Dictionary<NotificationEventCode, NotificationDescriptor>
            {
                // ── Customer: booking lifecycle ──
                [NotificationEventCode.BookingRequested] =
                    new(NotificationAudience.Customer, PushInApp, Standard, NotificationDestinationKind.Booking),

                [NotificationEventCode.BookingConfirmed] =
                    new(NotificationAudience.Customer, SmsPushInApp, Critical, NotificationDestinationKind.Booking),

                // The customer is otherwise left expecting an appointment that will not happen.
                [NotificationEventCode.BookingRejected] =
                    new(NotificationAudience.Customer, SmsPushInApp, Critical, NotificationDestinationKind.Booking),

                [NotificationEventCode.BookingRescheduled] =
                    new(NotificationAudience.Customer, SmsPushInApp, Critical, NotificationDestinationKind.Booking),

                [NotificationEventCode.BookingCancelledByProvider] =
                    new(NotificationAudience.Customer, SmsPushInApp, Critical, NotificationDestinationKind.Booking),

                // Their own action, echoed back. Nothing is lost by letting them mute it.
                [NotificationEventCode.BookingCancelledAck] =
                    new(NotificationAudience.Customer, PushInApp, Standard, NotificationDestinationKind.Booking),

                [NotificationEventCode.BookingReminder24h] =
                    new(NotificationAudience.Customer, PushInApp, Standard, NotificationDestinationKind.Booking),

                // The one reminder that carries an SMS, and the last chance to not be a no-show.
                [NotificationEventCode.BookingReminder2h] =
                    new(NotificationAudience.Customer, SmsPushInApp, Critical, NotificationDestinationKind.Booking),

                [NotificationEventCode.BookingCompleted] =
                    new(NotificationAudience.Customer, PushInApp, Standard, NotificationDestinationKind.Booking),

                [NotificationEventCode.ReviewRequest] =
                    new(NotificationAudience.Customer, PushInApp, Standard, NotificationDestinationKind.Booking),

                // Suppressible, like the ask it follows. Somebody who has switched review prompts off should
                // not receive the reminder either.
                [NotificationEventCode.ReviewReminder] =
                    new(NotificationAudience.Customer, PushInApp, Standard, NotificationDestinationKind.Booking),

                [NotificationEventCode.BookingNoShow] =
                    new(NotificationAudience.Customer, PushInApp, Standard, NotificationDestinationKind.Booking),


                // ── Customer: money. Records the customer is entitled to. ──
                [NotificationEventCode.PaymentReceived] =
                    new(NotificationAudience.Customer, SmsPushInApp, Critical, NotificationDestinationKind.Payment),

                [NotificationEventCode.PaymentFailed] =
                    new(NotificationAudience.Customer, SmsPush, Critical, NotificationDestinationKind.Payment),

                [NotificationEventCode.RefundProcessed] =
                    new(NotificationAudience.Customer, SmsPushInApp, Critical, NotificationDestinationKind.Payment),

                // ── Customer: account. Suppressing these strands someone mid-flow. ──
                [NotificationEventCode.PhoneVerification] =
                    new(NotificationAudience.Customer, NotificationChannel.SMS, Critical, NotificationDestinationKind.None),

                [NotificationEventCode.Welcome] =
                    new(NotificationAudience.Customer, PushInApp, Standard, NotificationDestinationKind.Profile),

                [NotificationEventCode.PasswordReset] =
                    new(NotificationAudience.Customer, NotificationChannel.SMS, Critical, NotificationDestinationKind.None),

                [NotificationEventCode.SecurityAlert] =
                    new(NotificationAudience.Customer, SmsPush, Critical, NotificationDestinationKind.Profile),

                // ── Provider: booking ──

                // Critical in the product sense: an unanswered request expires into lost revenue and a
                // customer left waiting. No SMS — the salon is an app user, unlike an invitee.
                [NotificationEventCode.NewBookingRequest] =
                    new(NotificationAudience.Provider, PushInApp, Critical, NotificationDestinationKind.Booking),

                [NotificationEventCode.NewBookingConfirmed] =
                    new(NotificationAudience.Provider, PushInApp, Standard, NotificationDestinationKind.Booking),

                // A hole in the day the salon has to fill, and only they can act on it.
                [NotificationEventCode.BookingCancelledByCustomer] =
                    new(NotificationAudience.Provider, PushInApp, Critical, NotificationDestinationKind.Booking),

                [NotificationEventCode.BookingRescheduledByCustomer] =
                    new(NotificationAudience.Provider, PushInApp, Standard, NotificationDestinationKind.Booking),

                [NotificationEventCode.CustomerNoShow] =
                    new(NotificationAudience.Provider, NotificationChannel.InApp, Standard, NotificationDestinationKind.Booking),

                [NotificationEventCode.NextAppointmentReminder] =
                    new(NotificationAudience.Provider, NotificationChannel.PushNotification, Standard, NotificationDestinationKind.Booking),

                [NotificationEventCode.DailyScheduleDigest] =
                    new(NotificationAudience.Provider, PushInApp, Standard, NotificationDestinationKind.None),

                // ── Reviews ──
                // In-app and push, standard, suppressible: news, not an emergency. No SMS — SMS is reserved for
                // critical notifications. The tap lands on the booking the review is about: the notification row
                // already carries it, the destination resolver already checks booking ownership, and a review is
                // only ever reached through its booking anyway.
                [NotificationEventCode.ReviewPublished] =
                    new(NotificationAudience.Provider, PushInApp, Standard, NotificationDestinationKind.Booking),

                [NotificationEventCode.ReviewRepublished] =
                    new(NotificationAudience.Provider, PushInApp, Standard, NotificationDestinationKind.Booking),

                [NotificationEventCode.ReviewReplyPublished] =
                    new(NotificationAudience.Customer, PushInApp, Standard, NotificationDestinationKind.Booking),

                // The author's own review was refused. Suppressible like the rest of the review family, and
                // pointed at the booking: "my reviews" is reached through it, and the reason is shown there too.
                [NotificationEventCode.ReviewRejected] =
                    new(NotificationAudience.Customer, PushInApp, Standard, NotificationDestinationKind.Booking),

                // ── Provider: staff and organisation ──

                // The invitee is often not a user yet, so SMS is the only way to reach them and they have no
                // preferences to honour.
                [NotificationEventCode.InvitationSent] =
                    new(NotificationAudience.StaffMember, SmsPush, Critical, NotificationDestinationKind.Invitation),

                [NotificationEventCode.InvitationAccepted] =
                    new(NotificationAudience.Provider, PushInApp, Standard, NotificationDestinationKind.Invitation),


                // Addressed to the member, not the salon: the owner performed the change and already knows.
                [NotificationEventCode.StaffAdded] =
                    new(NotificationAudience.StaffMember, PushInApp, Standard, NotificationDestinationKind.Provider),

                [NotificationEventCode.StaffRemoved] =
                    new(NotificationAudience.StaffMember, PushInApp, Standard, NotificationDestinationKind.Provider),

                [NotificationEventCode.StaffAssignedToBooking] =
                    new(NotificationAudience.StaffMember, PushInApp, Standard, NotificationDestinationKind.Booking),

                // ── Provider: money and account ──
                [NotificationEventCode.PayoutCompleted] =
                    new(NotificationAudience.Provider, SmsPushInApp, Critical, NotificationDestinationKind.Payout),




                [NotificationEventCode.ProviderActivated] =
                    new(NotificationAudience.Provider, PushInApp, Critical, NotificationDestinationKind.Provider),

            };

        /// <summary>Every code that has an entry.</summary>
        public static IEnumerable<NotificationEventCode> AllCodes => Entries.Keys;

        /// <summary>
        /// What <paramref name="code"/> is. Throws rather than defaulting: a notification with no stated
        /// audience or channels is a bug, and guessing would hide it.
        /// </summary>
        public static NotificationDescriptor Describe(NotificationEventCode code) =>
            Entries.TryGetValue(code, out var descriptor)
                ? descriptor
                : throw new KeyNotFoundException(
                    $"Notification '{code}' has no catalogue entry. Add one to {nameof(NotificationEventCatalog)} " +
                    "stating its audience, channels and criticality before raising it.");

        public static bool TryDescribe(NotificationEventCode code, out NotificationDescriptor? descriptor) =>
            Entries.TryGetValue(code, out descriptor);

        /// <summary>Whether the recipient's preferences may silence this notification.</summary>
        public static bool IsSuppressible(NotificationEventCode code) => Describe(code).IsSuppressible;

        /// <summary>
        /// The coarse label a notification carries once it is stored.
        /// </summary>
        /// <remarks>
        /// <para><see cref="NotificationEventCode"/> is the identity — one value per notification.
        /// <see cref="NotificationType"/> is the blunter label that goes onto the row, and it is what
        /// templates and the suppression policy are keyed by. Several codes map to one type, which is
        /// correct: the 24-hour and 2-hour reminders are both booking reminders.</para>
        ///
        /// <para><b>Renamed from <c>PreferenceCategoryFor</c> 2026-09-21.</b> The old name described a job
        /// it was not doing: its result is written to <c>Notifications.Type</c> by the outbox sweep, and
        /// nothing ever consulted it to decide whether a recipient wanted the notification. What a recipient
        /// toggles is now <see cref="NotificationPreferenceCategory"/>, a separate and genuinely-flags enum.
        /// Conflating the two is what produced the defect this section exists to repair.</para>
        ///
        /// <para>Mapped here rather than stored on the descriptor because it is a relationship to another
        /// type, not a property of the notification.</para>
        /// </remarks>
        public static NotificationType NotificationTypeFor(NotificationEventCode code) => code switch
        {
            NotificationEventCode.BookingRequested => NotificationType.NewBooking,
            NotificationEventCode.BookingConfirmed => NotificationType.BookingConfirmation,
            NotificationEventCode.BookingRejected => NotificationType.BookingCancellation,
            NotificationEventCode.BookingRescheduled => NotificationType.BookingRescheduled,
            NotificationEventCode.BookingCancelledByProvider => NotificationType.BookingCancelled,
            NotificationEventCode.BookingCancelledAck => NotificationType.BookingCancelled,
            NotificationEventCode.BookingReminder24h => NotificationType.BookingReminder,
            NotificationEventCode.BookingReminder2h => NotificationType.BookingReminder,
            NotificationEventCode.BookingCompleted => NotificationType.BookingUpdated,
            NotificationEventCode.ReviewRequest => NotificationType.ReviewRequest,
            NotificationEventCode.ReviewReminder => NotificationType.ReviewRequest,
            NotificationEventCode.BookingNoShow => NotificationType.BookingNoShow,

            NotificationEventCode.PaymentReceived => NotificationType.PaymentReceived,
            NotificationEventCode.PaymentFailed => NotificationType.PaymentFailed,
            NotificationEventCode.RefundProcessed => NotificationType.PaymentRefunded,

            NotificationEventCode.PhoneVerification => NotificationType.PhoneVerification,
            NotificationEventCode.Welcome => NotificationType.Welcome,
            NotificationEventCode.PasswordReset => NotificationType.PasswordReset,
            NotificationEventCode.SecurityAlert => NotificationType.SecurityAlert,

            NotificationEventCode.NewBookingRequest => NotificationType.NewBooking,
            NotificationEventCode.NewBookingConfirmed => NotificationType.NewBooking,
            NotificationEventCode.BookingCancelledByCustomer => NotificationType.BookingCancelled,
            NotificationEventCode.BookingRescheduledByCustomer => NotificationType.BookingRescheduled,
            NotificationEventCode.CustomerNoShow => NotificationType.ClientNoShow,
            NotificationEventCode.NextAppointmentReminder => NotificationType.BookingReminder,
            NotificationEventCode.DailyScheduleDigest => NotificationType.BusinessMetrics,

            NotificationEventCode.InvitationSent => NotificationType.StaffAssigned,
            NotificationEventCode.InvitationAccepted => NotificationType.StaffAssigned,
            NotificationEventCode.StaffAdded => NotificationType.StaffAssigned,
            NotificationEventCode.StaffRemoved => NotificationType.StaffUnavailable,
            NotificationEventCode.StaffAssignedToBooking => NotificationType.StaffAssigned,

            NotificationEventCode.ReviewPublished => NotificationType.NewReview,
            NotificationEventCode.ReviewRepublished => NotificationType.NewReview,
            NotificationEventCode.ReviewReplyPublished => NotificationType.ReviewResponse,
            NotificationEventCode.ReviewRejected => NotificationType.ReviewRejected,

            NotificationEventCode.PayoutCompleted => NotificationType.PayoutCompleted,
            NotificationEventCode.ProviderActivated => NotificationType.AccountUpdate,

            _ => throw new KeyNotFoundException(
                $"Notification '{code}' has no stored type. Add one to " +
                $"{nameof(NotificationTypeFor)} before it can be sent."),
        };
    }
}
