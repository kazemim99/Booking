// ========================================
// CreateZarinPalPaymentCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.CreateZarinPalPayment
{
    public sealed class CreateZarinPalPaymentCommandValidator : AbstractValidator<CreateZarinPalPaymentCommand>
    {
        public CreateZarinPalPaymentCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .WithMessage("Customer ID is required");

            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .WithMessage("Provider ID is required");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than zero")
                .GreaterThanOrEqualTo(1000)
                .WithMessage("Amount must be at least 1000 Rials for ZarinPal");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Currency is required")
                .Must(currency => currency == "IRR")
                .WithMessage("ZarinPal only supports IRR currency");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters");

            RuleFor(x => x.Mobile)
                .Matches(@"^09\d{9}$")
                .When(x => !string.IsNullOrEmpty(x.Mobile))
                .WithMessage("Mobile number must be a valid Iranian mobile number (09xxxxxxxxx)");

            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Email must be a valid email address");
        }
    }
}
