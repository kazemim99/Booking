// ========================================
// ZarinPalVerifyRequestDto.cs
// ========================================
using System.Text.Json.Serialization;

namespace Booksy.Infrastructure.External.Payment.ZarinPal.DTOs
{
    /// <summary>
    /// Request DTO for ZarinPal payment verification API
    /// </summary>
    public class ZarinPalVerifyRequestDto
    {
        [JsonPropertyName("merchant_id")]
        public string MerchantId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("authority")]
        public string Authority { get; set; } = string.Empty;
    }
}
