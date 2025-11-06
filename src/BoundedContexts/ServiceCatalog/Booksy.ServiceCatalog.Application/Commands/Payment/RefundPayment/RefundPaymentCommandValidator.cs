// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/RefundPayment/RefundPaymentCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.RefundPayment
{
    public sealed class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
    {
        public RefundPaymentCommandValidator()
        {
            RuleFor(x => x.PaymentId)
                .NotEmpty()
                .WithMessage("Payment ID is required");

            RuleFor(x => x.RefundAmount)
                .GreaterThan(0)
                .WithMessage("Refund amount must be greater than zero");

            RuleFor(x => x.Reason)
                .IsInEnum()
                .WithMessage("Invalid refund reason");
        }
    }
}
