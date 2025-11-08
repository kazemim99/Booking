// ========================================
// CreateBehpardakhtPaymentResponse.cs
// ========================================
namespace Booksy.ServiceCatalog.Api.Models.Responses
{
    /// <summary>
    /// Response model for Behpardakht payment creation
    /// </summary>
    public sealed class CreateBehpardakhtPaymentResponse
    {
        /// <summary>
        /// Payment ID
        /// </summary>
        public Guid PaymentId { get; set; }

        /// <summary>
        /// Behpardakht reference number
        /// </summary>
        public string RefId { get; set; } = string.Empty;

        /// <summary>
        /// Payment URL for redirection
        /// </summary>
        public string PaymentUrl { get; set; } = string.Empty;

        /// <summary>
        /// Payment amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Success indicator
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Error code if failed
        /// </summary>
        public int? ErrorCode { get; set; }
    }
}
