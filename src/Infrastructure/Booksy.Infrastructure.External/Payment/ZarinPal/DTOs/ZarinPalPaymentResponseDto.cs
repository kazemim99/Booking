// ========================================
// ZarinPalPaymentResponseDto.cs
// ========================================
using System.Text.Json.Serialization;

namespace Booksy.Infrastructure.External.Payment.ZarinPal.DTOs
{
    /// <summary>
    /// Response DTO for ZarinPal payment request API
    /// </summary>
    public class ZarinPalPaymentResponseDto
    {
        [JsonPropertyName("data")]
        public ZarinPalPaymentData? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<ZarinPalError>? Errors { get; set; }
    }

    public class ZarinPalPaymentData
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("authority")]
        public string Authority { get; set; } = string.Empty;

        [JsonPropertyName("fee_type")]
        public string? FeeType { get; set; }

        [JsonPropertyName("fee")]
        public long? Fee { get; set; }
    }

    public class ZarinPalError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("validations")]
        public List<ZarinPalValidation>? Validations { get; set; }
    }

    public class ZarinPalValidation
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
