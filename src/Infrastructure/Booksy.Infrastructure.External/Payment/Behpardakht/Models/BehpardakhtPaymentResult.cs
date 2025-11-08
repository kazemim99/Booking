// ========================================
// BehpardakhtPaymentResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.Behpardakht.Models
{
    /// <summary>
    /// Result of bpPayRequest operation
    /// </summary>
    public class BehpardakhtPaymentResult
    {
        public bool IsSuccessful { get; set; }
        public string? RefId { get; set; }
        public string? PaymentUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }
}
