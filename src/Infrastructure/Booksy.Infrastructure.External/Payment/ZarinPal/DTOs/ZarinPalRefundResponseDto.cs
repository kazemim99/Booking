// ========================================
// ZarinPalRefundResponseDto.cs
// ========================================
using System.Text.Json.Serialization;

namespace Booksy.Infrastructure.External.Payment.ZarinPal.DTOs
{
    /// <summary>
    /// Response DTO for ZarinPal refund API
    /// </summary>
    public class ZarinPalRefundResponseDto
    {
        [JsonPropertyName("data")]
        public ZarinPalRefundData? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<ZarinPalError>? Errors { get; set; }
    }

    public class ZarinPalRefundData
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
