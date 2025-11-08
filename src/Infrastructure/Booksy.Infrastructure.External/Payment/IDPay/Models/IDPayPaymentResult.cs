// ========================================
// IDPayPaymentResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.IDPay.Models
{
    /// <summary>
    /// Result of IDPay payment request creation
    /// </summary>
    public class IDPayPaymentResult
    {
        /// <summary>
        /// Whether the payment request was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Payment ID from IDPay
        /// </summary>
        public string? PaymentId { get; set; }

        /// <summary>
        /// Payment URL for redirection
        /// </summary>
        public string? PaymentUrl { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Error code if failed
        /// </summary>
        public int ErrorCode { get; set; }
    }
}
