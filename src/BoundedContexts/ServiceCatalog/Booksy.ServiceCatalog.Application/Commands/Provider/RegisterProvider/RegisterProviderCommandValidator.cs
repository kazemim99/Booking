// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderCommandValidator.cs
// ========================================
using FluentValidation;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider
{
    public sealed class RegisterProviderCommandValidator : AbstractValidator<RegisterProviderCommand>
    {
        public RegisterProviderCommandValidator()
        {
            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Owner ID is required");

            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .WithMessage("Business name is required")
                .MaximumLength(200)
                .WithMessage("Business name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.ProviderType)
                .IsInEnum()
                .WithMessage("Valid provider type is required");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Valid email address is required");

            RuleFor(x => x.PrimaryPhone)
                .NotEmpty()
                .WithMessage("Primary phone is required")
                .MinimumLength(10)
                .WithMessage("Phone number must be at least 10 digits");

            RuleFor(x => x.Street)
                .NotEmpty()
                .WithMessage("Street address is required")
                .MaximumLength(200)
                .WithMessage("Street address cannot exceed 200 characters");

            RuleFor(x => x.City)
                .NotEmpty()
                .WithMessage("City is required")
                .MaximumLength(100)
                .WithMessage("City cannot exceed 100 characters");

            RuleFor(x => x.State)
                .MaximumLength(100)
                .WithMessage("State cannot exceed 100 characters");

            RuleFor(x => x.PostalCode)
                .MaximumLength(20)
                .WithMessage("Postal code cannot exceed 20 characters");

            RuleFor(x => x.Country)
                .NotEmpty()
                .WithMessage("Country is required")
                .MaximumLength(100)
                .WithMessage("Country cannot exceed 100 characters");

            When(x => x.Latitude.HasValue, () =>
            {
                RuleFor(x => x.Latitude)
                    .InclusiveBetween(-90, 90)
                    .WithMessage("Latitude must be between -90 and 90 degrees");
            });

            When(x => x.Longitude.HasValue, () =>
            {
                RuleFor(x => x.Longitude)
                    .InclusiveBetween(-180, 180)
                    .WithMessage("Longitude must be between -180 and 180 degrees");
            });
        }
    }
}