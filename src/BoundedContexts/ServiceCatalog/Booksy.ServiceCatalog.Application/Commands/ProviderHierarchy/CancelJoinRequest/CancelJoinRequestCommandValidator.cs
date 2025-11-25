using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CancelJoinRequest
{
    public sealed class CancelJoinRequestCommandValidator : AbstractValidator<CancelJoinRequestCommand>
    {
        public CancelJoinRequestCommandValidator()
        {
            RuleFor(x => x.RequestId)
                .NotEmpty()
                .WithMessage("Request ID is required");

            RuleFor(x => x.RequesterId)
                .NotEmpty()
                .WithMessage("Requester ID is required");
        }
    }
}
