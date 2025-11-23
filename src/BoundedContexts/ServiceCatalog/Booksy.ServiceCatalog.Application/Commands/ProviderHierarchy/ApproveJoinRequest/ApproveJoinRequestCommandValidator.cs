using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ApproveJoinRequest;

public sealed class ApproveJoinRequestCommandValidator : AbstractValidator<ApproveJoinRequestCommand>
{
    public ApproveJoinRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Request ID is required");

        RuleFor(x => x.ReviewerId)
            .NotEmpty().WithMessage("Reviewer ID is required");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Note));
    }
}
