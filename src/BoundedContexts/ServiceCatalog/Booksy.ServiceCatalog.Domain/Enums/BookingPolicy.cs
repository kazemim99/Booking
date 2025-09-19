namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>
/// Defines different booking policies for cancellation and rescheduling
/// </summary>
public enum BookingPolicy
{
    /// <summary>
    /// No cancellation or rescheduling allowed once booked
    /// </summary>
    NoChanges = 1,

    /// <summary>
    /// Free cancellation and rescheduling up to 24 hours before appointment
    /// </summary>
    Flexible24Hours = 2,

    /// <summary>
    /// Free cancellation and rescheduling up to 48 hours before appointment
    /// </summary>
    Flexible48Hours = 3,

    /// <summary>
    /// Free cancellation and rescheduling up to 72 hours before appointment
    /// </summary>
    Flexible72Hours = 4,

    /// <summary>
    /// Free cancellation and rescheduling up to 1 week before appointment
    /// </summary>
    FlexibleOneWeek = 5,

    /// <summary>
    /// Cancellation allowed with 50% refund up to 24 hours before
    /// </summary>
    Moderate24Hours = 6,

    /// <summary>
    /// Cancellation allowed with 50% refund up to 48 hours before
    /// </summary>
    Moderate48Hours = 7,

    /// <summary>
    /// Strict policy - cancellation fee applies, rescheduling once allowed
    /// </summary>
    StrictWithFee = 8,

    /// <summary>
    /// Same-day cancellation allowed with full fee charge
    /// </summary>
    SameDayWithFee = 9,

    /// <summary>
    /// Emergency cancellations only (medical, family emergency)
    /// </summary>
    EmergencyOnly = 10,

    /// <summary>
    /// Custom policy defined by provider
    /// </summary>
    Custom = 11
}
