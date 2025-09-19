// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Service/BusinessAddressValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Validators.Common
{
    /// <summary>
    /// Validator for BusinessAddress value object
    /// </summary>
    public sealed class BusinessAddressValidator : AbstractValidator<BusinessAddress>
    {
        public BusinessAddressValidator()
        {
            RuleFor(x => x.Street)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Street address is required and cannot exceed 200 characters");

            RuleFor(x => x.City)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("City is required and cannot exceed 100 characters");

            RuleFor(x => x.PostalCode)
                .NotEmpty()
                .MaximumLength(20)
                .WithMessage("Postal code is required and cannot exceed 20 characters");

            RuleFor(x => x.Country)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Country is required and cannot exceed 100 characters");

            RuleFor(x => x.State)
                .MaximumLength(100)
                .WithMessage("State cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.State));

            When(x => x.Latitude.HasValue, () => {
                RuleFor(x => x.Latitude)
                    .InclusiveBetween(-90, 90)
                    .WithMessage("Latitude must be between -90 and 90 degrees");
            });

            When(x => x.Longitude.HasValue, () => {
                RuleFor(x => x.Longitude)
                    .InclusiveBetween(-180, 180)
                    .WithMessage("Longitude must be between -180 and 180 degrees");
            });
        }
    }
}