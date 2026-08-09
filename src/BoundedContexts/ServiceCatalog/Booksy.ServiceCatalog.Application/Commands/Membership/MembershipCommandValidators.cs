using Booksy.ServiceCatalog.Application.Commands.Membership.AcceptInvitationAsMember;
using Booksy.ServiceCatalog.Application.Commands.Membership.ChangeMembershipRoles;
using Booksy.ServiceCatalog.Application.Commands.Membership.RegisterAndAcceptInvitation;
using Booksy.ServiceCatalog.Application.Commands.Membership.RevokeInvitation;
using Booksy.ServiceCatalog.Application.Commands.Membership.TerminateMembership;
using Booksy.ServiceCatalog.Domain.Enums;
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Membership;

/// <summary>
/// Input validation for the membership commands. These run before the handler, so
/// malformed input is rejected at the boundary and the handlers stay focused on
/// authorization and domain invariants (which validation must never duplicate).
/// Column limits mirror the EF configuration so a request can never be rejected
/// by the database instead of by the API.
/// </summary>
public sealed class RevokeInvitationCommandValidator : AbstractValidator<RevokeInvitationCommand>
{
    public RevokeInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("Invitation ID is required");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}

public sealed class TerminateMembershipCommandValidator : AbstractValidator<TerminateMembershipCommand>
{
    public TerminateMembershipCommandValidator()
    {
        RuleFor(x => x.MembershipId)
            .NotEmpty().WithMessage("Membership ID is required");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}

public sealed class ChangeMembershipRolesCommandValidator : AbstractValidator<ChangeMembershipRolesCommand>
{
    public ChangeMembershipRolesCommandValidator()
    {
        RuleFor(x => x.MembershipId)
            .NotEmpty().WithMessage("Membership ID is required");

        RuleFor(x => x.Roles)
            .NotNull().WithMessage("At least one role is required")
            .Must(roles => roles is { Count: > 0 })
            .WithMessage("At least one role is required — terminate the membership instead of clearing its roles");

        RuleForEach(x => x.Roles)
            .Must(role => Enum.TryParse<MembershipRole>(role, ignoreCase: true, out _))
            .WithMessage(role => $"Unknown role '{role}'")
            .When(x => x.Roles is { Count: > 0 });
    }
}

public sealed class AcceptInvitationAsMemberCommandValidator : AbstractValidator<AcceptInvitationAsMemberCommand>
{
    public AcceptInvitationAsMemberCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("Invitation ID is required");
    }
}

public sealed class RegisterAndAcceptInvitationCommandValidator
    : AbstractValidator<RegisterAndAcceptInvitationCommand>
{
    public RegisterAndAcceptInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("Invitation ID is required");

        // The phone is taken from the invitation (the OTP proves ownership of it),
        // so only the person's own details are validated here.
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email is not valid")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("Verification code is required")
            .Matches("^[0-9]{4,8}$").WithMessage("Verification code must be 4–8 digits");
    }
}
