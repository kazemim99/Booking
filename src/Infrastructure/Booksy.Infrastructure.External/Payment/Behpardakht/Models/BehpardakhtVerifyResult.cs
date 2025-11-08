// ========================================
// BehpardakhtVerifyResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.Behpardakht.Models
{
    /// <summary>
    /// Result of bpVerifyRequest operation
    /// </summary>
    public class BehpardakhtVerifyResult
    {
        public bool IsSuccessful { get; set; }
        public long SaleReferenceId { get; set; }
        public string? CardHolderPan { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }
}
