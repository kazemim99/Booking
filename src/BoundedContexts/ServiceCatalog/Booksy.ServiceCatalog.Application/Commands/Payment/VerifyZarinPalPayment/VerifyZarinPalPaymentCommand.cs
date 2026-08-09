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
        string Status, Guid? IdempotencyKey = null)
        : ICommand<VerifyZarinPalPaymentResult>,
          Core.Application.Abstractions.CQRS.INonTransactionalCommand, // gateway verify must not run in a retried tx
          Core.Application.Abstractions.CQRS.IRequireIdempotency;      // at-most-once per idempotency key
}
