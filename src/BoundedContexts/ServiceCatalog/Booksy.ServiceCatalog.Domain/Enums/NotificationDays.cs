namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>
/// Days of the week for notification preferences
/// </summary>
[Flags]
public enum NotificationDays
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekends = Saturday | Sunday,
    All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
}
