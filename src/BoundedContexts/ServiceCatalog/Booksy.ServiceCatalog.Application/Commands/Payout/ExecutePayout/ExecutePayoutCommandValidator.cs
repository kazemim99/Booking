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
        }
    }
}
