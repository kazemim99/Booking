namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>
/// Types of notifications in the booking system
/// </summary>
[Flags]
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

    All = NewBooking | BookingCancelled | BookingRescheduled | BookingReminder | BookingConfirmation |
          PaymentReceived | PaymentFailed | PaymentRefunded |
          ScheduleChanged | TimeSlotAvailable | ScheduleConflict |
          NewReview | ReviewResponse |
          SystemMaintenance | AccountUpdate | SecurityAlert |
          Promotions | Newsletter |
          StaffAssigned | StaffUnavailable |
          ClientNoShow | ClientLateArrival |
          BusinessMetrics | LowInventory,
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
    AccountDeactivated = 16777232
}
