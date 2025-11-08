// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/States/CompletedState.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.States
{
    /// <summary>
    /// State: Booking has been completed successfully
    /// Terminal state - no further transitions allowed
    /// </summary>
    public sealed class CompletedState : BookingStateBase
    {
        public override BookingStatus Status => BookingStatus.Completed;

        // All operations throw InvalidBookingStateTransitionException (inherited from base)
    }
}
