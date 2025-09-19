// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Service/PriceValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Validators.Common
{
    /// <summary>
    /// Validator for Price value object
    /// </summary>
    public sealed class PriceValidator : AbstractValidator<Price>
    {
        public PriceValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Price amount must be greater than or equal to zero")
                .PrecisionScale(18, 2, true)
                .WithMessage("Price cannot have more than 2 decimal places");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Currency is required")
                .Length(3)
                .WithMessage("Currency must be a valid 3-letter ISO code")
                .Matches(@"^[A-Z]{3}$")
                .WithMessage("Currency must be uppercase letters only");
        }
    }
}