// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/AddStaff/AddStaffCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.AddStaff
{
    public sealed class AddStaffCommandValidator : AbstractValidator<AddStaffCommand>
    {
        public AddStaffCommandValidator()
        {
            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .WithMessage("Provider ID is required");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required")
                .MaximumLength(50)
                .WithMessage("First name cannot exceed 50 characters");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required")
                .MaximumLength(50)
                .WithMessage("Last name cannot exceed 50 characters");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Valid email address is required");

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("Valid staff role is required");

            When(x => !string.IsNullOrEmpty(x.Phone), () =>
            {
                RuleFor(x => x.Phone)
                    .MinimumLength(10)
                    .WithMessage("Phone number must be at least 10 digits");
            });
        }
    }
}