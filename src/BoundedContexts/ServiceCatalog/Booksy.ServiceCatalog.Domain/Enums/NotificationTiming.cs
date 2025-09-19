namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>
/// Timing preferences for notifications
/// </summary>
public enum NotificationTiming
{
    /// <summary>
    /// Immediate notification when event occurs
    /// </summary>
    Immediate = 1,

    /// <summary>
    /// 5 minutes before appointment
    /// </summary>
    FiveMinutesBefore = 2,

    /// <summary>
    /// 15 minutes before appointment
    /// </summary>
    FifteenMinutesBefore = 3,

    /// <summary>
    /// 30 minutes before appointment
    /// </summary>
    ThirtyMinutesBefore = 4,

    /// <summary>
    /// 1 hour before appointment
    /// </summary>
    OneHourBefore = 5,

    /// <summary>
    /// 2 hours before appointment
    /// </summary>
    TwoHoursBefore = 6,

    /// <summary>
    /// 24 hours (1 day) before appointment
    /// </summary>
    OneDayBefore = 7,

    /// <summary>
    /// 48 hours (2 days) before appointment
    /// </summary>
    TwoDaysBefore = 8,

    /// <summary>
    /// Weekly digest
    /// </summary>
    Weekly = 9,

    /// <summary>
    /// Daily summary
    /// </summary>
    Daily = 10,

    /// <summary>
    /// Never send notifications
    /// </summary>
    Never = 11
}
