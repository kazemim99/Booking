// ========================================
// IDPayVerifyResponseDto.cs
// ========================================
using System.Text.Json.Serialization;

namespace Booksy.Infrastructure.External.Payment.IDPay.DTOs
{
    /// <summary>
    /// Response DTO for IDPay payment verification
    /// </summary>
    public class IDPayVerifyResponseDto
    {
        /// <summary>
        /// Payment status code
        /// </summary>
        [JsonPropertyName("status")]
        public int Status { get; set; }

        /// <summary>
        /// Track ID from IDPay
        /// </summary>
        [JsonPropertyName("track_id")]
        public long TrackId { get; set; }

        /// <summary>
        /// Payment ID
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Order ID
        /// </summary>
        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }

        /// <summary>
        /// Amount in Rials
        /// </summary>
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        /// <summary>
        /// Card number (masked)
        /// </summary>
        [JsonPropertyName("card_no")]
        public string? CardNo { get; set; }

        /// <summary>
        /// Hashed card number
        /// </summary>
        [JsonPropertyName("hashed_card_no")]
        public string? HashedCardNo { get; set; }

        /// <summary>
        /// Payment date
        /// </summary>
        [JsonPropertyName("date")]
        public long Date { get; set; }
    }
}
