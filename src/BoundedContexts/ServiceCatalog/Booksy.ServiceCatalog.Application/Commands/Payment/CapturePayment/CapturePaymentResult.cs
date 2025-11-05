// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/CapturePayment/CapturePaymentResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Payment.CapturePayment
{
    public sealed record CapturePaymentResult(
        Guid PaymentId,
        decimal CapturedAmount,
        string Currency,
        string Status,
        DateTime CapturedAt);
}
