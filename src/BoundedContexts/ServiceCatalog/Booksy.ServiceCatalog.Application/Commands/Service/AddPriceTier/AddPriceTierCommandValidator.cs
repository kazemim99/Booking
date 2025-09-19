// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/AddPriceTier/AddPriceTierCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Service.AddPriceTier
{
    public sealed class AddPriceTierCommandValidator : AbstractValidator<AddPriceTierCommand>
    {
        public AddPriceTierCommandValidator()
        {
            RuleFor(x => x.ServiceId)
                .NotEmpty()
                .WithMessage("Service ID is required");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Price tier name is required")
                .MaximumLength(50)
                .WithMessage("Tier name cannot exceed 50 characters");

            When(x => !string.IsNullOrEmpty(x.Description), () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(200)
                    .WithMessage("Description cannot exceed 200 characters");
            });

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than 0")
                .PrecisionScale(2, 10,false)
                .WithMessage("Price cannot have more than 2 decimal places");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Currency is required")
                .Length(3)
                .WithMessage("Currency must be 3 characters (ISO code)");

            When(x => x.Attributes != null, () =>
            {
                RuleFor(x => x.Attributes)
                    .Must(attrs => attrs!.Count <= 10)
                    .WithMessage("Maximum 10 attributes allowed per tier");
            });
        }
    }
}