using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Xunit;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ValueObjects;

/// <summary>
/// P0-0 deposit policy. `BookingPolicy` is the single source of truth for whether money must be collected before a
/// booking can be confirmed, so these tests pin both deposit modes, the validation that stops an uncollectable
/// "required" deposit from being configured, and the cap that prevents charging more than the booking is worth.
/// </summary>
public class BookingPolicyDepositTests
{
    private static Money Irr(decimal amount) => Money.Create(amount, "IRR");

    private static BookingPolicy Policy(
        bool requireDeposit = true,
        DepositType type = DepositType.Percentage,
        decimal percentage = 20,
        decimal fixedAmount = 0) =>
        BookingPolicy.Create(
            minAdvanceBookingHours: 1,
            maxAdvanceBookingDays: 30,
            cancellationWindowHours: 24,
            cancellationFeePercentage: 0,
            allowRescheduling: true,
            rescheduleWindowHours: 24,
            requireDeposit: requireDeposit,
            depositPercentage: percentage,
            depositType: type,
            depositFixedAmount: fixedAmount);

    // ---------------------------------------------------------------- percentage mode

    [Fact]
    public void Percentage_deposit_takes_the_configured_share_of_the_total()
    {
        var deposit = Policy(percentage: 20).CalculateDepositAmount(Irr(1_000_000));

        Assert.Equal(200_000m, deposit.Amount);
        Assert.Equal("IRR", deposit.Currency);
    }

    [Fact]
    public void Percentage_deposit_of_one_hundred_equals_the_total()
    {
        Assert.Equal(1_000_000m, Policy(percentage: 100).CalculateDepositAmount(Irr(1_000_000)).Amount);
    }

    // ---------------------------------------------------------------- fixed mode

    [Fact]
    public void Fixed_deposit_takes_the_flat_amount_regardless_of_total()
    {
        var policy = Policy(type: DepositType.FixedAmount, percentage: 0, fixedAmount: 150_000);

        Assert.Equal(150_000m, policy.CalculateDepositAmount(Irr(1_000_000)).Amount);
        Assert.Equal(150_000m, policy.CalculateDepositAmount(Irr(500_000)).Amount);
    }

    [Fact]
    public void Fixed_deposit_is_capped_at_the_booking_total()
    {
        // A flat amount configured against a cheaper service must never exceed what is being booked.
        var policy = Policy(type: DepositType.FixedAmount, percentage: 0, fixedAmount: 900_000);

        Assert.Equal(300_000m, policy.CalculateDepositAmount(Irr(300_000)).Amount);
    }

    // ---------------------------------------------------------------- no deposit

    [Fact]
    public void No_deposit_required_yields_zero()
    {
        Assert.Equal(0m, Policy(requireDeposit: false, percentage: 0).CalculateDepositAmount(Irr(1_000_000)).Amount);
    }

    [Fact]
    public void The_platform_default_policy_requires_no_deposit()
    {
        Assert.False(BookingPolicy.Default.RequireDeposit);
        Assert.Equal(0m, BookingPolicy.Default.CalculateDepositAmount(Irr(1_000_000)).Amount);
    }

    // ---------------------------------------------------------------- invalid combinations

    [Fact]
    public void A_required_percentage_deposit_of_zero_is_rejected()
    {
        // Otherwise a provider could "require" a deposit of nothing, leaving bookings permanently unconfirmable.
        Assert.Throws<ArgumentException>(() => Policy(type: DepositType.Percentage, percentage: 0));
    }

    [Fact]
    public void A_required_fixed_deposit_of_zero_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Policy(type: DepositType.FixedAmount, percentage: 0, fixedAmount: 0));
    }

    [Fact]
    public void A_negative_fixed_amount_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            Policy(type: DepositType.FixedAmount, percentage: 0, fixedAmount: -1));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void An_out_of_range_percentage_is_rejected(decimal percentage)
    {
        Assert.Throws<ArgumentException>(() => Policy(percentage: percentage));
    }

    [Fact]
    public void Percentage_mode_ignores_a_stray_fixed_amount_and_vice_versa()
    {
        // Only the selected mode's value is used, so a leftover value from the other mode cannot leak into the charge.
        Assert.Equal(200_000m,
            Policy(type: DepositType.Percentage, percentage: 20, fixedAmount: 999_999)
                .CalculateDepositAmount(Irr(1_000_000)).Amount);

        Assert.Equal(50_000m,
            Policy(type: DepositType.FixedAmount, percentage: 99, fixedAmount: 50_000)
                .CalculateDepositAmount(Irr(1_000_000)).Amount);
    }

    // ---------------------------------------------------------------- backward compatibility

    [Fact]
    public void Deposit_type_defaults_to_percentage_so_existing_policies_keep_their_meaning()
    {
        var policy = BookingPolicy.Create(
            minAdvanceBookingHours: 1, maxAdvanceBookingDays: 30, cancellationWindowHours: 24,
            cancellationFeePercentage: 0, allowRescheduling: true, rescheduleWindowHours: 24,
            requireDeposit: true, depositPercentage: 25);

        Assert.Equal(DepositType.Percentage, policy.DepositType);
        Assert.Equal(250_000m, policy.CalculateDepositAmount(Irr(1_000_000)).Amount);
    }

    [Fact]
    public void Deposit_terms_participate_in_equality()
    {
        var a = Policy(type: DepositType.FixedAmount, percentage: 0, fixedAmount: 100);
        var b = Policy(type: DepositType.FixedAmount, percentage: 0, fixedAmount: 200);

        Assert.NotEqual(a, b);
    }
}
