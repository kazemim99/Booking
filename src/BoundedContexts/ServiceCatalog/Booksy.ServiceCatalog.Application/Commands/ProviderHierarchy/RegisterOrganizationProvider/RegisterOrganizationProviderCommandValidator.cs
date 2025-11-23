using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RegisterOrganizationProvider;

public sealed class RegisterOrganizationProviderCommandValidator : AbstractValidator<RegisterOrganizationProviderCommand>
{
    public RegisterOrganizationProviderCommandValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Business name is required")
            .MaximumLength(100).WithMessage("Business name cannot exceed 100 characters");

        RuleFor(x => x.BusinessDescription)
            .NotEmpty().WithMessage("Business description is required")
            .MaximumLength(1000).WithMessage("Business description cannot exceed 1000 characters");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(50).WithMessage("Category cannot exceed 50 characters");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .MinimumLength(10).WithMessage("Phone number must be at least 10 digits")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address is required")
            .MaximumLength(200).WithMessage("Address cannot exceed 200 characters");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(200).WithMessage("Address line 2 cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.AddressLine2));

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.Province)
            .NotEmpty().WithMessage("Province is required")
            .MaximumLength(100).WithMessage("Province cannot exceed 100 characters");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required")
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180");

        RuleFor(x => x.OwnerFirstName)
            .NotEmpty().WithMessage("Owner first name is required")
            .MaximumLength(50).WithMessage("Owner first name cannot exceed 50 characters");

        RuleFor(x => x.OwnerLastName)
            .NotEmpty().WithMessage("Owner last name is required")
            .MaximumLength(50).WithMessage("Owner last name cannot exceed 50 characters");
    }
}
