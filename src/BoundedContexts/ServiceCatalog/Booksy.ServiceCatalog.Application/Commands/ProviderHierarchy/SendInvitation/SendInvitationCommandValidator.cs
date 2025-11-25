using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.SendInvitation;

public sealed class SendInvitationCommandValidator : AbstractValidator<SendInvitationCommand>
{
    public SendInvitationCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .MinimumLength(10).WithMessage("Phone number must be at least 10 digits")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters");

        RuleFor(x => x.InviteeName)
            .MaximumLength(100).WithMessage("Invitee name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.InviteeName));

        RuleFor(x => x.Message)
            .MaximumLength(500).WithMessage("Message cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Message));
    }
}
