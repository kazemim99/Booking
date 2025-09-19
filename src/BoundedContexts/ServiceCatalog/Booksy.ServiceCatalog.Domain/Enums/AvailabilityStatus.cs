namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>
/// Represents the availability status of providers, staff, or time slots
/// </summary>
public enum AvailabilityStatus
{
    /// <summary>
    /// Available for booking - normal operating state
    /// </summary>
    Available = 1,

    /// <summary>
    /// Currently busy with an existing appointment
    /// </summary>
    Busy = 2,

    /// <summary>
    /// Temporarily blocked/unavailable (break, lunch, personal time)
    /// </summary>
    Blocked = 3,

    /// <summary>
    /// On scheduled break
    /// </summary>
    OnBreak = 4,

    /// <summary>
    /// Out to lunch
    /// </summary>
    AtLunch = 5,

    /// <summary>
    /// Out of office (vacation, sick leave, etc.)
    /// </summary>
    OutOfOffice = 6,

    /// <summary>
    /// Offline or not working today
    /// </summary>
    Offline = 7,

    /// <summary>
    /// In training or meeting
    /// </summary>
    InTraining = 8,

    /// <summary>
    /// Tentatively booked (pending confirmation)
    /// </summary>
    Tentative = 9,

    /// <summary>
    /// Available but with limited capacity
    /// </summary>
    LimitedAvailability = 10,

    /// <summary>
    /// Emergency unavailable (unexpected absence)
    /// </summary>
    Emergency = 11,

    /// <summary>
    /// Preparing for next appointment (buffer time)
    /// </summary>
    Preparing = 12,

    /// <summary>
    /// Overtime work (available but outside normal hours)
    /// </summary>
    Overtime = 13,

    /// <summary>
    /// On call - available for urgent appointments only
    /// </summary>
    OnCall = 14,

    /// <summary>
    /// Maintenance or setup time (equipment, room preparation)
    /// </summary>
    Maintenance = 15
}
