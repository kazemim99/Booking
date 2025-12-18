using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ConvertToOrganization;

public sealed class ConvertToOrganizationCommandValidator : AbstractValidator<ConvertToOrganizationCommand>
{
    public ConvertToOrganizationCommandValidator()
    {
        RuleFor(x => x.ProviderId)
            .NotEmpty().WithMessage("Provider ID is required");
    }
}
