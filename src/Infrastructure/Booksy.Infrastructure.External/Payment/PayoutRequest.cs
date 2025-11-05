// ========================================
// Booksy.Infrastructure.External/Payment/PayoutRequest.cs
// ========================================
namespace Booksy.Infrastructure.External.Payment
{
    public sealed class PayoutRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = default!;
        public string? ConnectedAccountId { get; set; }
        public string? BankAccountId { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
