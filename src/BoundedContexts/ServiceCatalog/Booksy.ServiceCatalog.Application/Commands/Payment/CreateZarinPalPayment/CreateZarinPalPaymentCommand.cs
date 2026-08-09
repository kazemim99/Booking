// ========================================
// CreateZarinPalPaymentCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.CreateZarinPalPayment
{
    /// <summary>
    /// Command to create a ZarinPal payment request
    /// </summary>
    public sealed record CreateZarinPalPaymentCommand(
        Guid? BookingId,
        Guid CustomerId,
        Guid ProviderId,
        decimal Amount,
        string Currency,
        string? Description = null,
        string? Mobile = null,
        string? Email = null,
        Dictionary<string, object>? Metadata = null,
        Guid? IdempotencyKey = null)
        : ICommand<CreateZarinPalPaymentResult>,
          Core.Application.Abstractions.CQRS.INonTransactionalCommand, // gateway request must not run in a retried tx
          Core.Application.Abstractions.CQRS.IRequireIdempotency;      // at-most-once per idempotency key
}
