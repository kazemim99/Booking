// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/RefundPayment/RefundPaymentCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.RefundPayment
{
    public sealed record RefundPaymentCommand(
        Guid PaymentId,
        decimal RefundAmount,
        RefundReason Reason,
        string? Notes = null,
        Guid? IdempotencyKey = null) : ICommand<RefundPaymentResult>;
}
