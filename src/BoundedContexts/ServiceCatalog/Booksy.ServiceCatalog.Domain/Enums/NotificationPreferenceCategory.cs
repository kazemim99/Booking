namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>
/// The coarse buckets a recipient can switch on and off. A genuine flags enum: every member is a distinct
/// bit, so a mask means exactly what it says.
/// </summary>
/// <remarks>
/// <para><b>Why this exists.</b> <see cref="NotificationType"/> was doing two incompatible jobs at once: it
/// was the discriminator persisted on every notification and template, AND the mask a user's preferences are
/// stored as. Those pull in opposite directions — a discriminator wants one value per notification and grows
/// with the product, a mask wants distinct bits and cannot exceed 32 of them. Growing the discriminator
/// eventually added members as consecutive integers under a <c>[Flags]</c> attribute, which is where the
/// false positives came from: <c>RefundIssued</c> bitwise-"contained" <c>RefundProcessed</c>, and rendering a
/// mask by name dropped types the user had enabled while inventing ones they had not.</para>
///
/// <para><b>Why the names and numbers are copied exactly.</b> Preferences are persisted as text
/// (<c>HasConversion&lt;string&gt;()</c>) — a stored row reads "All", or
/// "BookingReminder, BookingConfirmation, PaymentReceived". Keeping every name and every value identical to
/// the bit-valued half of <see cref="NotificationType"/> means every stored preference parses to the same
/// set it always did, so this separation needs no data migration. Changing a name here later would not be a
/// rename; it would be a migration.</para>
///
/// <para><b>Adding a member</b> means the next free bit, never the next integer. Bits 24–31 are unused; once
/// they are gone, this enum is full, and that is the signal to reconsider the buckets rather than to start
/// counting.</para>
/// </remarks>
[Flags]
public enum NotificationPreferenceCategory
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

    /// <summary>
    /// Every bucket. Unlike its predecessor this really is all of them, because there is nothing outside the
    /// bits — the members that used to sit beyond <c>All</c> were notification identities, and identity now
    /// lives in <see cref="NotificationEventCode"/>.
    /// </summary>
    All = NewBooking | BookingCancelled | BookingRescheduled | BookingReminder | BookingConfirmation |
          PaymentReceived | PaymentFailed | PaymentRefunded |
          ScheduleChanged | TimeSlotAvailable | ScheduleConflict |
          NewReview | ReviewResponse |
          SystemMaintenance | AccountUpdate | SecurityAlert |
          Promotions | Newsletter |
          StaffAssigned | StaffUnavailable |
          ClientNoShow | ClientLateArrival |
          BusinessMetrics | LowInventory,
}
