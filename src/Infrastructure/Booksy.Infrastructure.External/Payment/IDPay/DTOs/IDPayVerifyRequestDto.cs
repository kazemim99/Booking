// ========================================
// IDPayVerifyRequestDto.cs
// ========================================
using System.Text.Json.Serialization;

namespace Booksy.Infrastructure.External.Payment.IDPay.DTOs
{
    /// <summary>
    /// Request DTO for verifying an IDPay payment
    /// </summary>
    public class IDPayVerifyRequestDto
    {
        /// <summary>
        /// Payment ID from IDPay
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Order ID
        /// </summary>
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;
    }
}
