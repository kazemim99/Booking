using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.AcceptInvitation;

public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty().WithMessage("Invitation ID is required");

        RuleFor(x => x.IndividualProviderId)
            .NotEmpty().WithMessage("Individual provider ID is required");
    }
}
