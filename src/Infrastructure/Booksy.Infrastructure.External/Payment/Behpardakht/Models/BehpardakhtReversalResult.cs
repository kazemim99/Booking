// ========================================
// BehpardakhtReversalResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment.Behpardakht.Models
{
    /// <summary>
    /// Result of bpReversalRequest operation
    /// </summary>
    public class BehpardakhtReversalResult
    {
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }
}
