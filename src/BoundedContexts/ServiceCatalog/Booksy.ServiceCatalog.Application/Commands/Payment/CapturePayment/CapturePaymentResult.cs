// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/CapturePayment/CapturePaymentResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Payment.CapturePayment
{
    public sealed record CapturePaymentResult(
        Guid PaymentId,
        Guid BookingId,
        Guid CustomerId,
        Guid ProviderId,
        decimal Amount,
        string Currency,
        string Status,
        string PaymentMethod,
        string? PaymentIntentId,
        DateTime CapturedAt,
        DateTime CreatedAt,
        bool IsSuccessful,
        string? ErrorMessage);
}
