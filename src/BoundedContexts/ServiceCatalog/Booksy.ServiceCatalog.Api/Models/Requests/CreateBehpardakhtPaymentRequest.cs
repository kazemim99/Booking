// ========================================
// CreateBehpardakhtPaymentRequest.cs
// ========================================
namespace Booksy.ServiceCatalog.API.Models.Requests
{
    /// <summary>
    /// Request model for creating a Behpardakht payment
    /// </summary>
    public sealed class CreateBehpardakhtPaymentRequest
    {
        /// <summary>
        /// Booking ID (optional for direct payments)
        /// </summary>
        public Guid? BookingId { get; set; }

        /// <summary>
        /// Provider ID
        /// </summary>
        public Guid ProviderId { get; set; }

        /// <summary>
        /// Payment amount in Rials
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Payment description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Customer mobile number
        /// </summary>
        public string? Mobile { get; set; }

        /// <summary>
        /// Customer email
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Currency code (e.g., IRR)
        /// </summary>
        public string Currency { get; set; } = "IRR";

        /// <summary>
        /// Payer ID (optional)
        /// </summary>
        public long PayerId { get; set; }

        /// <summary>
        /// Additional data for the payment
        /// </summary>
        public string? AdditionalData { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
