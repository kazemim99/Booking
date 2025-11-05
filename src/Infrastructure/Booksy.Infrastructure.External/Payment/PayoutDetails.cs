// ========================================
// Booksy.Infrastructure.External/Payment/PayoutDetails.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment
{
    public sealed class PayoutDetails
    {
        public string PayoutId { get; set; } = default!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public string? BankAccountLast4 { get; set; }
        public string? BankName { get; set; }
        public string? FailureMessage { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
