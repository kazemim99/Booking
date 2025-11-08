// ========================================
// CreateZarinPalPaymentRequest.cs
// ========================================
namespace Booksy.ServiceCatalog.API.Models.Requests
{
    /// <summary>
    /// Request model for creating a ZarinPal payment
    /// </summary>
    public sealed class CreateZarinPalPaymentRequest
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
        /// Customer mobile number (09xxxxxxxxx)
        /// </summary>
        public string? Mobile { get; set; }

        /// <summary>
        /// Customer email
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
