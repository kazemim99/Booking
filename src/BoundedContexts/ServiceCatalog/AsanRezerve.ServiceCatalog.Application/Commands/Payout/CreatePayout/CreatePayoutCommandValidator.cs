// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Payout/CreatePayout/CreatePayoutCommandValidator.cs
// ========================================
using FluentValidation;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Payout.CreatePayout
{
    public sealed class CreatePayoutCommandValidator : AbstractValidator<CreatePayoutCommand>
    {
        public CreatePayoutCommandValidator()
        {
            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .WithMessage("Provider ID is required");

            RuleFor(x => x.PeriodStart)
                .NotEmpty()
                .WithMessage("Period start date is required");

            RuleFor(x => x.PeriodEnd)
                .NotEmpty()
                .GreaterThan(x => x.PeriodStart)
                .WithMessage("Period end must be after period start");

            // A period that has not finished cannot be paid out: the ledger balance the payout is
            // derived from would include charges from beyond the period the payout claims to cover.
            RuleFor(x => x.PeriodEnd)
                .LessThanOrEqualTo(_ => DateTime.UtcNow)
                .WithMessage("Period end cannot be in the future");

            RuleFor(x => x.CommissionPercentage)
                .InclusiveBetween(0, 100)
                .When(x => x.CommissionPercentage.HasValue)
                .WithMessage("Commission percentage must be between 0 and 100");

            RuleFor(x => x.ScheduledAt)
                .GreaterThan(DateTime.UtcNow)
                .When(x => x.ScheduledAt.HasValue)
                .WithMessage("Scheduled date must be in the future");
        }
    }
}
