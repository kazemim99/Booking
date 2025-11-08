// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/UpdateCustomerProfile/UpdateCustomerProfileCommandValidator.cs
// ========================================
using FluentValidation;
using System.Text.RegularExpressions;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.UpdateCustomerProfile
{
    /// <summary>
    /// Validator for UpdateCustomerProfileCommand
    /// </summary>
    public sealed class UpdateCustomerProfileCommandValidator : AbstractValidator<UpdateCustomerProfileCommand>
    {
        public UpdateCustomerProfileCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("CustomerId is required");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

            RuleFor(x => x.MiddleName)
                .MaximumLength(100).WithMessage("Middle name cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.MiddleName));

            // Iranian phone number validation
            RuleFor(x => x.PhoneNumber)
                .Must(BeValidIranianPhoneNumber)
                .WithMessage("Invalid Iranian phone number format. Use +98xxxxxxxxxx or 09xxxxxxxxx")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.Today.AddYears(-13))
                .WithMessage("Customer must be at least 13 years old")
                .When(x => x.DateOfBirth.HasValue);

            RuleFor(x => x.Gender)
                .Must(g => g == null || new[] { "Male", "Female", "Other", "PreferNotToSay" }.Contains(g))
                .WithMessage("Invalid gender value");

            When(x => x.Address != null, () =>
            {
                RuleFor(x => x.Address!.Street)
                    .NotEmpty().WithMessage("Street is required")
                    .MaximumLength(200).WithMessage("Street cannot exceed 200 characters");

                RuleFor(x => x.Address!.City)
                    .NotEmpty().WithMessage("City is required")
                    .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

                RuleFor(x => x.Address!.State)
                    .NotEmpty().WithMessage("State is required")
                    .MaximumLength(100).WithMessage("State cannot exceed 100 characters");

                RuleFor(x => x.Address!.PostalCode)
                    .NotEmpty().WithMessage("Postal code is required")
                    .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters");

                RuleFor(x => x.Address!.Country)
                    .NotEmpty().WithMessage("Country is required")
                    .MaximumLength(100).WithMessage("Country cannot exceed 100 characters");
            });

            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Bio));

            RuleFor(x => x.AvatarUrl)
                .Must(BeValidUrl)
                .WithMessage("Invalid avatar URL")
                .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
        }

        private bool BeValidIranianPhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true;

            var cleaned = Regex.Replace(phoneNumber, @"[\s\-]", string.Empty);

            // International format: +98 followed by 10 digits starting with 9
            if (Regex.IsMatch(cleaned, @"^\+989\d{9}$"))
                return true;

            // National format: 09 followed by 9 digits
            if (Regex.IsMatch(cleaned, @"^09\d{9}$"))
                return true;

            return false;
        }

        private bool BeValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
