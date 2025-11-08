// ========================================
// Booksy.UserManagement.Application/CQRS/Commands/Customer/RegisterCustomer/RegisterCustomerCommandValidator.cs
// ========================================
using FluentValidation;
using System.Text.RegularExpressions;

namespace Booksy.UserManagement.Application.CQRS.Commands.Customer.RegisterCustomer
{
    /// <summary>
    /// Validator for RegisterCustomerCommand with Iranian phone number support
    /// </summary>
    public sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
    {
        public RegisterCustomerCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
                .MaximumLength(128).WithMessage("Password cannot exceed 128 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

            RuleFor(x => x.MiddleName)
                .MaximumLength(100).WithMessage("Middle name cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.MiddleName));

            // Iranian phone number validation: +98xxxxxxxxxx or 09xxxxxxxxx
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .Must(BeValidIranianPhoneNumber)
                .WithMessage("Invalid Iranian phone number format. Use +98xxxxxxxxxx or 09xxxxxxxxx");

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
        }

        /// <summary>
        /// Validates Iranian phone number formats:
        /// - +98xxxxxxxxxx (international format)
        /// - 09xxxxxxxxx (national format)
        /// </summary>
        private bool BeValidIranianPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove spaces and dashes
            var cleaned = Regex.Replace(phoneNumber, @"[\s\-]", string.Empty);

            // International format: +98 followed by 10 digits starting with 9
            if (Regex.IsMatch(cleaned, @"^\+989\d{9}$"))
                return true;

            // National format: 09 followed by 9 digits
            if (Regex.IsMatch(cleaned, @"^09\d{9}$"))
                return true;

            return false;
        }
    }
}
