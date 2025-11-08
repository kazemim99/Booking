// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/PaymentAggregate/States/RefundedState.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate.States
{
    /// <summary>
    /// State: Payment has been fully refunded
    /// Terminal state - no further transitions allowed
    /// </summary>
    public sealed class RefundedState : PaymentStateBase
    {
        public override PaymentStatus Status => PaymentStatus.Refunded;

        // All operations throw InvalidPaymentStateTransitionException (inherited from base)
    }
}
