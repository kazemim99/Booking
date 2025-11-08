// ========================================
// IDPayPaymentRequestDto.cs
// ========================================
using System.Text.Json.Serialization;

namespace Booksy.Infrastructure.External.Payment.IDPay.DTOs
{
    /// <summary>
    /// Request DTO for creating an IDPay payment
    /// </summary>
    public class IDPayPaymentRequestDto
    {
        /// <summary>
        /// Order ID (unique identifier)
        /// </summary>
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// Amount in Rials
        /// </summary>
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        /// <summary>
        /// Payment description
        /// </summary>
        [JsonPropertyName("desc")]
        public string? Description { get; set; }

        /// <summary>
        /// Callback URL
        /// </summary>
        [JsonPropertyName("callback")]
        public string CallbackUrl { get; set; } = string.Empty;

        /// <summary>
        /// Payer name (optional)
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Payer phone (optional)
        /// </summary>
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        /// <summary>
        /// Payer email (optional)
        /// </summary>
        [JsonPropertyName("mail")]
        public string? Email { get; set; }
    }
}
