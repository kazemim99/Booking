// ========================================
// IDPayVerifyResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.IDPay.Models
{
    /// <summary>
    /// Result of IDPay payment verification
    /// </summary>
    public class IDPayVerifyResult
    {
        /// <summary>
        /// Whether the verification was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Track ID from IDPay
        /// </summary>
        public long TrackId { get; set; }

        /// <summary>
        /// Payment ID
        /// </summary>
        public string? PaymentId { get; set; }

        /// <summary>
        /// Amount in Rials
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// Card number (masked)
        /// </summary>
        public string? CardNumber { get; set; }

        /// <summary>
        /// Hashed card number
        /// </summary>
        public string? HashedCardNumber { get; set; }

        /// <summary>
        /// Payment status code
        /// </summary>
        public int StatusCode { get; set; }

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
