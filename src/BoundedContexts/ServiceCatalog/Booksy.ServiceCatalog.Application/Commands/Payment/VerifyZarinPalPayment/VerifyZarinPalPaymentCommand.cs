// ========================================
// VerifyZarinPalPaymentCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.VerifyZarinPalPayment
{
    /// <summary>
    /// Command to verify a ZarinPal payment after callback
    /// </summary>
    public sealed record VerifyZarinPalPaymentCommand(
        string Authority,
        string Status, Guid? IdempotencyKey =null) : ICommand<VerifyZarinPalPaymentResult>;
}
