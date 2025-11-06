// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/ProcessPayment/ProcessPaymentCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.ProcessPayment
{
    public sealed record ProcessPaymentCommand(
        Guid? BookingId,
        Guid CustomerId,
        Guid ProviderId,
        decimal Amount,
        string Currency,
        PaymentMethod Method,
        string PaymentMethodId,
        string? Description = null,
        Dictionary<string, object>? Metadata = null,
        Guid? IdempotencyKey = null) : ICommand<ProcessPaymentResult>;
}
