// ========================================
// Booksy.Infrastructure.External/Payment/PayoutResult.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment
{
    public sealed class PayoutResult
    {
        public bool IsSuccessful { get; set; }
        public string? PayoutId { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
    }
}
