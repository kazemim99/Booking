// ========================================
// BehpardakhtSettleResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.Behpardakht.Models
{
    /// <summary>
    /// Result of bpSettleRequest operation
    /// </summary>
    public class BehpardakhtSettleResult
    {
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }
}
