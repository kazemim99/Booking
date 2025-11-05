namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Represents the payment status for a booking
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// No payment has been made yet
        /// </summary>
        Pending,

        /// <summary>
        /// Partial payment (deposit) has been made
        /// </summary>
        PartiallyPaid,

        /// <summary>
        /// Full payment has been completed
        /// </summary>
        Paid,

        /// <summary>
        /// Payment has been refunded
        /// </summary>
        Refunded,

        /// <summary>
        /// Partial refund has been processed
        /// </summary>
        PartiallyRefunded,

        /// <summary>
        /// Payment failed
        /// </summary>
        Failed
    }
}
