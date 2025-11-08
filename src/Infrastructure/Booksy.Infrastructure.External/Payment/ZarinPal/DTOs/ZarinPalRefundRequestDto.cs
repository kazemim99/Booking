// ========================================
// ZarinPalRefundRequestDto.cs
// ========================================
using System.Text.Json.Serialization;

namespace Booksy.Infrastructure.External.Payment.ZarinPal.DTOs
{
    /// <summary>
    /// Request DTO for ZarinPal refund API
    /// </summary>
    public class ZarinPalRefundRequestDto
    {
        [JsonPropertyName("merchant_id")]
        public string MerchantId { get; set; } = string.Empty;

        [JsonPropertyName("authority")]
        public string Authority { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
