// ========================================
// Booksy.ServiceCatalog.Domain/Enums/PayoutStatus.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Represents the status of a provider payout
    /// </summary>
    public enum PayoutStatus
    {
        /// <summary>
        /// Payout is scheduled but not yet processed
        /// </summary>
        Pending,

        /// <summary>
        /// Payout is being processed
        /// </summary>
        Processing,

        /// <summary>
        /// Payout has been sent successfully
        /// </summary>
        Paid,

        /// <summary>
        /// Payout failed (bank rejection, invalid account, etc.)
        /// </summary>
        Failed,

        /// <summary>
        /// Payout was cancelled before processing
        /// </summary>
        Cancelled,

        /// <summary>
        /// Payout is on hold (fraud review, compliance check)
        /// </summary>
        OnHold
    }
}
