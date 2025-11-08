// ========================================
// ZarinPalVerifyResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.ZarinPal.Models
{
    /// <summary>
    /// Result of ZarinPal payment verification operation
    /// </summary>
    public class ZarinPalVerifyResult
    {
        public bool IsSuccessful { get; set; }
        public long RefId { get; set; }
        public string? CardPan { get; set; }
        public string? CardHash { get; set; }
        public decimal? Fee { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }
}
