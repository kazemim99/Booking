// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/States/BookingStateBase.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Exceptions;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.States
{
    /// <summary>
    /// Base class for booking states - provides default implementations that throw exceptions
    /// </summary>
    public abstract class BookingStateBase : IBookingState
    {
        public abstract BookingStatus Status { get; }

        public virtual void Confirm(Booking booking)
        {
            throw new InvalidBookingStateTransitionException(Status, "Confirm");
        }

        public virtual void Cancel(Booking booking, string reason, bool byProvider = false)
        {
            throw new InvalidBookingStateTransitionException(Status, "Cancel");
        }

        public virtual void Complete(Booking booking, string? staffNotes = null)
        {
            throw new InvalidBookingStateTransitionException(Status, "Complete");
        }

        public virtual Booking Reschedule(Booking booking, DateTime newStartTime, Guid newStaffId, string? reason = null)
        {
            throw new InvalidBookingStateTransitionException(Status, "Reschedule");
        }

        public virtual void MarkAsNoShow(Booking booking, string? reason = null)
        {
            throw new InvalidBookingStateTransitionException(Status, "MarkAsNoShow");
        }
    }
}
