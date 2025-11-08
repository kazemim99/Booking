// ========================================
// ZarinPalRefundResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.ZarinPal.Models
{
    /// <summary>
    /// Result of ZarinPal refund operation
    /// </summary>
    public class ZarinPalRefundResult
    {
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }
}
