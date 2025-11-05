// ========================================
// Booksy.UserManagement.Application/Commands/RegisterUser/RegisterUserCommand.cs
// ========================================
using FluentValidation;

namespace Booksy.UserManagement.Application.CQRS.Commands.RegisterUser
{
    /// <summary>
    /// Validator for RegisterUserCommand
    /// </summary>
    public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {

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

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.Today.AddYears(-13))
                .WithMessage("User must be at least 13 years old")
                .When(x => x.DateOfBirth.HasValue);

            RuleFor(x => x.Gender)
                .Must(g => g == null || new[] { "Male", "Female", "Other", "PreferNotToSay" }.Contains(g))
                .WithMessage("Invalid gender value");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.UserType)
                .IsInEnum().WithMessage("Invalid user type");

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
                .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters");

            RuleFor(x => x.PreferredLanguage)
                .Matches(@"^[a-z]{2}(-[A-Z]{2})?$")
                .WithMessage("Invalid language code format (e.g., 'en' or 'en-US')")
                .When(x => !string.IsNullOrEmpty(x.PreferredLanguage));

            RuleFor(x => x.TimeZone)
                .Must(BeValidTimeZone)
                .WithMessage("Invalid time zone")
                .When(x => !string.IsNullOrEmpty(x.TimeZone));
        }

        private bool BeValidTimeZone(string? timeZone)
        {
            if (string.IsNullOrEmpty(timeZone))
                return true;

            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

