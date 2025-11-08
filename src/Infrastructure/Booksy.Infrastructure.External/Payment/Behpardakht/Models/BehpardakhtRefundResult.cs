// ========================================
// BehpardakhtRefundResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.Behpardakht.Models
{
    /// <summary>
    /// Result of bpRefundRequest operation
    /// </summary>
    public class BehpardakhtRefundResult
    {
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }
}
