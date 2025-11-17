namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Represents the availability status of a time slot
    /// </summary>
    public enum AvailabilityStatus
    {
        /// <summary>
        /// Time slot is available for booking
        /// </summary>
        Available,

        /// <summary>
        /// Time slot has been booked by a customer
        /// </summary>
        Booked,

        /// <summary>
        /// Time slot is blocked by provider (vacation, personal time, etc.)
        /// </summary>
        Blocked,

        /// <summary>
        /// Time slot is during break period (lunch, prayer, etc.)
        /// </summary>
        Break,

        /// <summary>
        /// Time slot is tentatively held during booking process (5-15 min hold)
        /// </summary>
        TentativeHold
    }
}
