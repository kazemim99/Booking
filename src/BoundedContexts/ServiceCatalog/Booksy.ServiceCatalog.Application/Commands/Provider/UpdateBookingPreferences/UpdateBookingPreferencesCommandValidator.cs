using Booksy.ServiceCatalog.Domain.Enums;
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBookingPreferences;

/// <summary>
/// Validates a booking-policy change before it reaches the domain. Deposit rules are money rules, so an invalid
/// combination is rejected at the boundary with a clear message rather than surfacing as a domain exception:
/// a "required" deposit whose amount is zero would leave every booking permanently unconfirmable.
/// </summary>
public sealed class UpdateBookingPreferencesCommandValidator : AbstractValidator<UpdateBookingPreferencesCommand>
{
    public UpdateBookingPreferencesCommandValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty();

        RuleFor(x => x.DepositPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Deposit percentage must be between 0 and 100.");

        RuleFor(x => x.DepositFixedAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Deposit amount cannot be negative.");

        // When a deposit is required, the *selected* mode must carry a usable value.
        When(x => x.RequireDeposit && x.DepositType == DepositType.Percentage, () =>
        {
            RuleFor(x => x.DepositPercentage)
                .GreaterThan(0)
                .WithMessage("A percentage deposit requires a deposit percentage greater than 0.");
        });

        When(x => x.RequireDeposit && x.DepositType == DepositType.FixedAmount, () =>
        {
            RuleFor(x => x.DepositFixedAmount)
                .GreaterThan(0)
                .WithMessage("A fixed-amount deposit requires an amount greater than 0.");
        });

        RuleFor(x => x.MinAdvanceBookingHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxAdvanceBookingDays).GreaterThanOrEqualTo(1);
        RuleFor(x => x.CancellationWindowHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CancellationFeePercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.RescheduleWindowHours).GreaterThanOrEqualTo(0);
    }
}
