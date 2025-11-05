// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/RefundPayment/RefundPaymentResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Payment.RefundPayment
{
    public sealed record RefundPaymentResult(
        Guid PaymentId,
        decimal RefundAmount,
        string Currency,
        string RefundId,
        string Status,
        DateTime RefundedAt);
}
