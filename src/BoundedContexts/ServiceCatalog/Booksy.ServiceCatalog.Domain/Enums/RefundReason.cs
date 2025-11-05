// ========================================
// Booksy.ServiceCatalog.Domain/Enums/RefundReason.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Represents the reason for a refund
    /// </summary>
    public enum RefundReason
    {
        /// <summary>
        /// Customer requested cancellation
        /// </summary>
        CustomerCancellation,

        /// <summary>
        /// Provider cancelled the booking
        /// </summary>
        ProviderCancellation,

        /// <summary>
        /// Service not delivered as expected
        /// </summary>
        ServiceNotDelivered,

        /// <summary>
        /// Customer was not satisfied with service
        /// </summary>
        CustomerDissatisfaction,

        /// <summary>
        /// Duplicate payment
        /// </summary>
        Duplicate,

        /// <summary>
        /// Fraudulent transaction
        /// </summary>
        Fraud,

        /// <summary>
        /// Payment dispute
        /// </summary>
        Dispute,

        /// <summary>
        /// Other reason
        /// </summary>
        Other
    }
}
