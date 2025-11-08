// ========================================
// VerifyZarinPalPaymentCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.VerifyZarinPalPayment
{
    public sealed class VerifyZarinPalPaymentCommandValidator : AbstractValidator<VerifyZarinPalPaymentCommand>
    {
        public VerifyZarinPalPaymentCommandValidator()
        {
            RuleFor(x => x.Authority)
                .NotEmpty()
                .WithMessage("Authority is required")
                .MinimumLength(10)
                .WithMessage("Authority must be at least 10 characters");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required")
                .Must(status => status == "OK" || status == "NOK")
                .WithMessage("Status must be either 'OK' or 'NOK'");
        }
    }
}
