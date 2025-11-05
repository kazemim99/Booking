// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/CapturePayment/CapturePaymentCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.CapturePayment
{
    /// <summary>
    /// Command to capture a previously authorized payment
    /// </summary>
    public sealed record CapturePaymentCommand(
        Guid PaymentId,
        decimal? AmountToCapture = null,
        Guid? IdempotencyKey = null) : ICommand<CapturePaymentResult>;
}
