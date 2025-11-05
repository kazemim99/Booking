namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Represents the current status of a booking in the system
    /// </summary>
    public enum BookingStatus
    {
        /// <summary>
        /// Booking has been requested but not yet confirmed
        /// </summary>
        Requested,

        /// <summary>
        /// Booking has been confirmed and scheduled
        /// </summary>
        Confirmed,

        /// <summary>
        /// Booking has been cancelled by customer or provider
        /// </summary>
        Cancelled,

        /// <summary>
        /// Booking has been completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Customer did not show up for the appointment
        /// </summary>
        NoShow,

        /// <summary>
        /// Booking has been rescheduled to a different time
        /// </summary>
        Rescheduled
    }
}
