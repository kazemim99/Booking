using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.AcceptInvitationWithRegistration;

public sealed class AcceptInvitationWithRegistrationCommandValidator : AbstractValidator<AcceptInvitationWithRegistrationCommand>
{
    public AcceptInvitationWithRegistrationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty()
            .WithMessage("InvitationId is required");

        RuleFor(x => x.OrganizationId)
            .NotEmpty()
            .WithMessage("OrganizationId is required");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required")
            .Matches(@"^\+98\d{10}$")
            .WithMessage("Phone number must be in format +98XXXXXXXXXX");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MinimumLength(2)
            .WithMessage("First name must be at least 2 characters")
            .MaximumLength(50)
            .WithMessage("First name must not exceed 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MinimumLength(2)
            .WithMessage("Last name must be at least 2 characters")
            .MaximumLength(50)
            .WithMessage("Last name must not exceed 50 characters");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Invalid email format");

        RuleFor(x => x.OtpCode)
            .NotEmpty()
            .WithMessage("OTP code is required")
            .Matches(@"^\d{6}$")
            .WithMessage("OTP code must be exactly 6 digits");
    }
}
