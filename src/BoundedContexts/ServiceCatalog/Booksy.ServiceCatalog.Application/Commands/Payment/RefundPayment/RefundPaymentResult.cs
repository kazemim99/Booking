// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/RefundPayment/RefundPaymentResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Payment.RefundPayment
{
    public sealed record RefundPaymentResult(
        Guid PaymentId,
        Guid BookingId,
        Guid CustomerId,
        Guid ProviderId,
        decimal Amount,
        string Currency,
        string Status,
        string PaymentMethod,
        string? PaymentIntentId,
        DateTime RefundedAt,
        DateTime CreatedAt,
        bool IsSuccessful,
        string? ErrorMessage);
}
