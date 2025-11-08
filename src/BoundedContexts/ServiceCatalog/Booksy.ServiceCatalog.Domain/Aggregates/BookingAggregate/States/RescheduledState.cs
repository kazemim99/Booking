// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/States/RescheduledState.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.States
{
    /// <summary>
    /// State: Booking has been rescheduled to a different time
    /// Terminal state - no further transitions allowed (new booking created)
    /// </summary>
    public sealed class RescheduledState : BookingStateBase
    {
        public override BookingStatus Status => BookingStatus.Rescheduled;

        // All operations throw InvalidBookingStateTransitionException (inherited from base)
    }
}
