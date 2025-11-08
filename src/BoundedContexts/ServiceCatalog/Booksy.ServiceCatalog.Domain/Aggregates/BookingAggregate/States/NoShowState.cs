// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/States/NoShowState.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.States
{
    /// <summary>
    /// State: Customer did not show up for the appointment
    /// Terminal state - no further transitions allowed
    /// </summary>
    public sealed class NoShowState : BookingStateBase
    {
        public override BookingStatus Status => BookingStatus.NoShow;

        // All operations throw InvalidBookingStateTransitionException (inherited from base)
    }
}
