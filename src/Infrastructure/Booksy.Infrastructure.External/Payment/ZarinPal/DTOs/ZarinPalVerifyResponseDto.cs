// ========================================
// ZarinPalVerifyResponseDto.cs
// ========================================
using System.Text.Json.Serialization;

namespace Booksy.Infrastructure.External.Payment.ZarinPal.DTOs
{
    /// <summary>
    /// Response DTO for ZarinPal payment verification API
    /// </summary>
    public class ZarinPalVerifyResponseDto
    {
        [JsonPropertyName("data")]
        public ZarinPalVerifyData? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<ZarinPalError>? Errors { get; set; }
    }

    public class ZarinPalVerifyData
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("card_hash")]
        public string? CardHash { get; set; }

        [JsonPropertyName("card_pan")]
        public string? CardPan { get; set; }

        [JsonPropertyName("ref_id")]
        public long RefId { get; set; }

        [JsonPropertyName("fee_type")]
        public string? FeeType { get; set; }

        [JsonPropertyName("fee")]
        public long? Fee { get; set; }
    }
}
