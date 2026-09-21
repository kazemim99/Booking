namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>
/// Stable identity of one notification the product can send.
/// </summary>
/// <remarks>
/// <para>This is the key the notification catalogue is indexed by: audience, channels, criticality,
/// suppressibility and tap destination are all looked up from the code, so a notification cannot be raised
/// without someone having stated what it is. It is also what a client selects its presentation from, which is
/// why the values must stay stable across releases — a client that has shipped will keep sending back the
/// name it was given.</para>
///
/// <para><b>Deliberately not <c>[Flags]</c>.</b> This is a plain identity: exactly one value per
/// notification, compared by equality and never masked. <see cref="NotificationType"/> used to carry that
/// attribute while its members from 16777216 up were sequential integers rather than distinct bits — so
/// <c>HasFlag</c> reported false positives between them and rendering a mask by name dropped categories a
/// user had enabled. It has since lost the attribute and kept its narrower job, the coarse label a stored
/// notification carries; the mask a user actually toggles is
/// <see cref="NotificationPreferenceCategory"/>.</para>
///
/// <para>Only codes the product actually sends belong here. Adding one without a catalogue entry fails the
/// catalogue's self-validation test, and adding one with no emitter advertises a notification the backend
/// can never send.</para>
/// </remarks>
public enum NotificationEventCode
{
    /// <summary>Unset. Never valid on a raised notification; present so a default(T) is obviously wrong.</summary>
    None = 0,

    // ── Customer: booking lifecycle ──

    /// <summary>The customer's request was recorded and is waiting on the salon's decision.</summary>
    BookingRequested = 1,

    BookingConfirmed = 2,

    /// <summary>The salon declined the request. Critical: the customer is otherwise left expecting an appointment.</summary>
    BookingRejected = 3,

    BookingRescheduled = 4,

    /// <summary>The salon cancelled. Distinct from <see cref="BookingCancelledAck"/> because the party who
    /// did not act is the one who needs telling.</summary>
    BookingCancelledByProvider = 5,

    /// <summary>The customer cancelled, and this is their receipt for it.</summary>
    BookingCancelledAck = 6,

    BookingReminder24h = 7,

    /// <summary>The late reminder. Critical, and the one reminder that carries an SMS.</summary>
    BookingReminder2h = 8,

    BookingCompleted = 9,
    ReviewRequest = 10,
    BookingNoShow = 11,

    /// <summary>A deposit is owed before the booking is held.</summary>
    DepositRequired = 12,

    /// <summary>The deposit window is about to close.</summary>
    PaymentDeadlineReminder = 13,

    /// <summary>
    /// The single follow-up to <see cref="ReviewRequest"/>, three days later, for a customer who has not
    /// reviewed. A separate code rather than a second <see cref="ReviewRequest"/> because the wording must
    /// differ — repeating the first message verbatim reads to the recipient as a bug, not a reminder.
    /// </summary>
    ReviewReminder = 14,

    // ── Customer: money ──

    PaymentReceived = 20,
    PaymentFailed = 21,
    RefundProcessed = 22,

    // ── Customer: account ──

    /// <summary>One-time code. Non-suppressible: suppressing it strands the person mid-sign-in.</summary>
    PhoneVerification = 30,

    Welcome = 31,
    PasswordReset = 32,
    SecurityAlert = 33,

    // ── Provider: booking ──

    /// <summary>A request is waiting on the salon's decision. Critical in the product sense: an unanswered
    /// request expires into lost revenue and a customer left waiting.</summary>
    NewBookingRequest = 40,

    NewBookingConfirmed = 41,

    /// <summary>The customer cancelled, leaving a hole in the salon's day.</summary>
    BookingCancelledByCustomer = 42,

    BookingRescheduledByCustomer = 43,
    CustomerNoShow = 44,

    /// <summary>The salon's own nudge before their next appointment.</summary>
    NextAppointmentReminder = 45,

    DailyScheduleDigest = 46,

    // ── Provider: staff and organisation ──

    InvitationSent = 50,
    InvitationAccepted = 51,

    // 52 was JoinRequestApproved. Removed 2026-09-21: nothing in the codebase creates or approves a join
    // request — the flow is an orphan event, a status enum and a table from an old migration. A code with
    // no emitter advertises a notification the product cannot send, which is the one thing this catalogue
    // exists to prevent. Add it back in the same change that builds the flow; the number is left unused so
    // an old persisted row cannot silently become a different notification.
    StaffAdded = 53,
    StaffRemoved = 54,

    /// <summary>Addressed to the staff member who was put on the booking, not to the salon.</summary>
    StaffAssignedToBooking = 55,

    // ── Provider: money and account ──

    PayoutCompleted = 60,
    PayoutFailed = 61,
    PayoutOnHold = 62,
    InvoiceGenerated = 63,
    ProviderVerificationChanged = 64,
    ProviderActivated = 65,
    ProviderDeactivated = 66,
}
