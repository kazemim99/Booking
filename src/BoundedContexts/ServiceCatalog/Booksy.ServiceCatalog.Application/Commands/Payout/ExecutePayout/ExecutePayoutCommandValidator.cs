// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payout/ExecutePayout/ExecutePayoutCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Payout.ExecutePayout
{
    public sealed class ExecutePayoutCommandValidator : AbstractValidator<ExecutePayoutCommand>
    {
        public ExecutePayoutCommandValidator()
        {
            RuleFor(x => x.PayoutId)
                .NotEmpty()
                .WithMessage("Payout ID is required");

            // The destination account is what the money is sent to; the gateway request cannot be
            // built without it, so an empty one is a bad request rather than a failed payout.
            RuleFor(x => x.ConnectedAccountId)
                .NotEmpty()
                .WithMessage("Connected account ID is required");
        }
    }
}
