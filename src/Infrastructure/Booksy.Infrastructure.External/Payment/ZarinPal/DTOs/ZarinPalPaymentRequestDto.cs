// ========================================
// ZarinPalPaymentRequestDto.cs
// ========================================
using System.Text.Json.Serialization;

namespace Booksy.Infrastructure.External.Payment.ZarinPal.DTOs
{
    /// <summary>
    /// Request DTO for ZarinPal payment request API
    /// </summary>
    public class ZarinPalPaymentRequestDto
    {
        [JsonPropertyName("merchant_id")]
        public string MerchantId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("callback_url")]
        public string CallbackUrl { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public ZarinPalMetadata? Metadata { get; set; }
    }

    public class ZarinPalMetadata
    {
        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}
