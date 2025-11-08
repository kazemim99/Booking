// ========================================
// IDPayPaymentResponseDto.cs
// ========================================
using System.Text.Json.Serialization;

namespace Booksy.Infrastructure.External.Payment.IDPay.DTOs
{
    /// <summary>
    /// Response DTO for IDPay payment creation
    /// </summary>
    public class IDPayPaymentResponseDto
    {
        /// <summary>
        /// Payment ID from IDPay
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Payment link for redirection
        /// </summary>
        [JsonPropertyName("link")]
        public string? Link { get; set; }
    }
}
