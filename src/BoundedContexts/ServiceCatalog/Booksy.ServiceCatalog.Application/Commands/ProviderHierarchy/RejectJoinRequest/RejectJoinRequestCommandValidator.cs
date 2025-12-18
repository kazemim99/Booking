using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RejectJoinRequest;

public sealed class RejectJoinRequestCommandValidator : AbstractValidator<RejectJoinRequestCommand>
{
    public RejectJoinRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Request ID is required");

        RuleFor(x => x.ReviewerId)
            .NotEmpty().WithMessage("Reviewer ID is required");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
