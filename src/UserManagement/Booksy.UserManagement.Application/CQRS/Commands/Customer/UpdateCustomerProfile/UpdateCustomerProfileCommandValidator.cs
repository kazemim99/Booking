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


            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");


            //// Iranian phone number validation
            //RuleFor(x => x.PhoneNumber)
            //    .Must(BeValidIranianPhoneNumber)
            //    .WithMessage("Invalid Iranian phone number format. Use +98xxxxxxxxxx or 09xxxxxxxxx")
            //    .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

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
