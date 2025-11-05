// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/ProcessPayment/ProcessPaymentResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Payment.ProcessPayment
{
    public sealed record ProcessPaymentResult(
        Guid PaymentId,
        Guid CustomerId,
        Guid ProviderId,
        decimal Amount,
        string Currency,
        string Status,
        string? PaymentIntentId,
        DateTime ProcessedAt);
}
