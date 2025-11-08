// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/States/CancelledState.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.States
{
    /// <summary>
    /// State: Booking has been cancelled by customer or provider
    /// Terminal state - no further transitions allowed
    /// </summary>
    public sealed class CancelledState : BookingStateBase
    {
        public override BookingStatus Status => BookingStatus.Cancelled;

        // All operations throw InvalidBookingStateTransitionException (inherited from base)
    }
}
