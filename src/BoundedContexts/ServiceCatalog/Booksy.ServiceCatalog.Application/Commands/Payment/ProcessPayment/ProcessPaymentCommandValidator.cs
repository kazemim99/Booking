// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/ProcessPayment/ProcessPaymentCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.ProcessPayment
{
    public sealed class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
    {
        public ProcessPaymentCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .WithMessage("Customer ID is required");

            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .WithMessage("Provider ID is required");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than zero");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .Length(3)
                .WithMessage("Currency must be a 3-letter ISO code");

            RuleFor(x => x.Method)
                .IsInEnum()
                .WithMessage("Invalid payment method");

            RuleFor(x => x.PaymentMethodId)
                .NotEmpty()
                .WithMessage("Payment method ID is required");
        }
    }
}
