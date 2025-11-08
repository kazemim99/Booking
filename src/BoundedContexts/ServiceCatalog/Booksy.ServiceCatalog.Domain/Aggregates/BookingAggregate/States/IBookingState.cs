// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/States/IBookingState.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.States
{
    /// <summary>
    /// Interface for Booking State pattern - defines state-specific behavior
    /// </summary>
    public interface IBookingState
    {
        /// <summary>
        /// Gets the booking status this state represents
        /// </summary>
        BookingStatus Status { get; }

        /// <summary>
        /// Confirms the booking
        /// </summary>
        void Confirm(Booking booking);

        /// <summary>
        /// Cancels the booking with a reason
        /// </summary>
        void Cancel(Booking booking, string reason, bool byProvider = false);

        /// <summary>
        /// Completes the booking after service is provided
        /// </summary>
        void Complete(Booking booking, string? staffNotes = null);

        /// <summary>
        /// Reschedules the booking to a new time
        /// </summary>
        Booking Reschedule(Booking booking, DateTime newStartTime, Guid newStaffId, string? reason = null);

        /// <summary>
        /// Marks the booking as no-show
        /// </summary>
        void MarkAsNoShow(Booking booking, string? reason = null);
    }
}
