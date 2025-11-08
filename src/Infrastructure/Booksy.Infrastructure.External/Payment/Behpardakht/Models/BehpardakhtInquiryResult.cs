// ========================================
// BehpardakhtInquiryResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.Behpardakht.Models
{
    /// <summary>
    /// Result of bpInquiryRequest operation
    /// </summary>
    public class BehpardakhtInquiryResult
    {
        public bool IsSuccessful { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }
}
