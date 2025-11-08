// ========================================
// ZarinPalPaymentResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.ZarinPal.Models
{
    /// <summary>
    /// Result of ZarinPal payment request operation
    /// </summary>
    public class ZarinPalPaymentResult
    {
        public bool IsSuccessful { get; set; }
        public string Authority { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public decimal? Fee { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }
}
