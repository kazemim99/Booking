using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RemoveStaffMember;

public sealed class RemoveStaffMemberCommandValidator : AbstractValidator<RemoveStaffMemberCommand>
{
    public RemoveStaffMemberCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required");

        RuleFor(x => x.StaffProviderId)
            .NotEmpty().WithMessage("Staff provider ID is required");

        RuleFor(x => x.OrganizationId)
            .NotEqual(x => x.StaffProviderId)
            .WithMessage("Organization cannot remove itself as staff");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason for removal is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}
