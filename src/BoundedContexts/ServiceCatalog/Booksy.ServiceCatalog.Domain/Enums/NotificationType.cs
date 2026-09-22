namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>
/// What kind of notification a stored notification or template is. A discriminator: exactly one value per
/// row, compared by equality.
/// </summary>
/// <remarks>
/// <para><b>Deliberately no longer <c>[Flags]</c>.</b> It never was one in fact — the members from 16777216
/// up are consecutive integers, not distinct bits, so <c>HasFlag</c> reported nonsense between them
/// (<c>RefundIssued</c> "contained" <c>RefundProcessed</c>) and <c>ToString()</c> decomposed a combination
/// greedily, dropping names that were set and inventing ones that were not. The attribute was the lie; the
/// values are fine for what this enum actually is.</para>
///
/// <para><b>Nothing here may be renamed or renumbered.</b> This is persisted as TEXT on both
/// <c>Notifications.Type</c> and <c>NotificationTemplates.Type</c>, and the outbox sweep writes it on every
/// notification it sends. A removed name is a row that can no longer be read.</para>
///
/// <para>The mask a recipient's preferences are stored as moved to
/// <see cref="NotificationPreferenceCategory"/>, which is a real flags enum. Which notification something is
/// — its identity — is <see cref="NotificationEventCode"/>. This type is neither of those: it is the coarse
/// label a row carries.</para>
/// </remarks>
public enum NotificationType
{
    None = 0,

    // Booking-related
    NewBooking = 1,
    BookingCancelled = 2,
    BookingRescheduled = 4,
    BookingReminder = 8,
    BookingConfirmation = 16,

    // Payment-related
    PaymentReceived = 32,
    PaymentFailed = 64,
    PaymentRefunded = 128,

    // Schedule-related
    ScheduleChanged = 256,
    TimeSlotAvailable = 512,
    ScheduleConflict = 1024,

    // Review-related
    NewReview = 2048,
    ReviewResponse = 4096,

    // System-related
    SystemMaintenance = 8192,
    AccountUpdate = 16384,
    SecurityAlert = 32768,

    // Marketing-related
    Promotions = 65536,
    Newsletter = 131072,

    // Staff-related
    StaffAssigned = 262144,
    StaffUnavailable = 524288,

    // Client-related
    ClientNoShow = 1048576,
    ClientLateArrival = 2097152,

    // Business-related
    BusinessMetrics = 4194304,
    LowInventory = 8388608,

    // `All` used to sit here. It was a mask over the members above, which is a preferences idea, not a
    // discriminator one — no notification or template row has ever carried it. It lives on as
    // NotificationPreferenceCategory.All, with the same name and the same value, so stored preference text
    // reading "All" parses exactly as before.

    ReviewRequest = 16777216,
    RefundProcessed = 16777217,
    BookingCancellation = 16777218,
    BookingNoShow = 16777219,
    PayoutCompleted = 16777220,
    SystemAlert = 16777221,
    Welcome = 16777222,
    RefundIssued = 16777223,
    PayoutProcessed = 16777224,
    InvoiceGenerated = 16777225,
    PhoneVerification = 16777226,
    PaymentConfirmed = 16777227,
    PasswordReset = 16777228,
    BookingUpdated = 16777229,
    BookingConfirmed = 16777230,
    AccountVerification = 16777231,
    AccountDeactivated = 16777232,

    /// <summary>A customer's own review was refused by moderation. Stored as its name, so no migration.</summary>
    ReviewRejected = 16777233
}
