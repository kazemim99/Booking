// ========================================
// Booksy.ServiceCatalog.Domain/Enums/TransactionType.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Represents the type of financial transaction
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Charge customer's payment method
        /// </summary>
        Charge,

        /// <summary>
        /// Authorize payment (hold funds)
        /// </summary>
        Authorization,

        /// <summary>
        /// Capture previously authorized payment
        /// </summary>
        Capture,

        /// <summary>
        /// Refund to customer
        /// </summary>
        Refund,

        /// <summary>
        /// Partial refund to customer
        /// </summary>
        PartialRefund,

        /// <summary>
        /// Payment dispute raised by customer
        /// </summary>
        Dispute,

        /// <summary>
        /// Chargeback from payment provider
        /// </summary>
        Chargeback,

        /// <summary>
        /// Payout to service provider
        /// </summary>
        Payout,

        /// <summary>
        /// Commission deduction
        /// </summary>
        Commission,

        /// <summary>
        /// Adjustment (manual correction)
        /// </summary>
        Adjustment
    }
}
