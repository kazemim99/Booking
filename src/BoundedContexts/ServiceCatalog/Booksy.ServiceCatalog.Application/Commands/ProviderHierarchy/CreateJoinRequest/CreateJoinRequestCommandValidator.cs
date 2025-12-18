using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CreateJoinRequest;

public sealed class CreateJoinRequestCommandValidator : AbstractValidator<CreateJoinRequestCommand>
{
    public CreateJoinRequestCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required");

        RuleFor(x => x.RequesterId)
            .NotEmpty().WithMessage("Requester ID is required");

        RuleFor(x => x.OrganizationId)
            .NotEqual(x => x.RequesterId)
            .WithMessage("Cannot request to join your own organization");

        RuleFor(x => x.Message)
            .MaximumLength(500).WithMessage("Message cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Message));
    }
}
