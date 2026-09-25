using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.BookingAggregate;

/// <summary>
/// A booking keeps the discount it was made with (openspec/changes/add-discounts-and-campaigns). Its total is the
/// discounted price — the salon funds every discount — so deposit and cancellation fee follow the total unchanged.
/// </summary>
public class BookingDiscountTests
{
    private static readonly DateTime Start = SalonTime.Now.Date.AddDays(10).AddHours(11);

    private static BookingPolicy Policy(bool deposit = false) => BookingPolicy.Create(
        minAdvanceBookingHours: 1, maxAdvanceBookingDays: 90, cancellationWindowHours: 24,
        cancellationFeePercentage: 50m, allowRescheduling: true, rescheduleWindowHours: 24,
        requireDeposit: deposit, depositPercentage: 20m);

    private static AppliedDiscount Discount(decimal amount = 200_000m) =>
        new(Guid.NewGuid(), "تخفیف پاییزه", "AUTUMN", PromotionOwner.Provider, amount);

    private static Booking Book(AppliedDiscount? discount, bool deposit = false) =>
        Booking.CreateBookingRequest(
            UserId.From(Guid.NewGuid()), ProviderId.New(), ServiceId.New(), Guid.NewGuid(),
            Start, Duration.FromMinutes(60), Price.Create(1_000_000m, "IRT"), Policy(deposit),
            discount: discount);

    [Fact]
    public void Without_a_discount_the_total_is_the_list_price()
    {
        var booking = Book(null);

        Assert.Null(booking.Discount);
        Assert.Equal(1_000_000m, booking.TotalPrice.Amount);
        Assert.Equal(1_000_000m, booking.SubtotalAmount);
    }

    [Fact]
    public void A_discounted_booking_costs_the_subtotal_minus_the_discount()
    {
        var discount = Discount();

        var booking = Book(discount);

        Assert.Equal(800_000m, booking.TotalPrice.Amount);
        Assert.Equal(1_000_000m, booking.SubtotalAmount);
        Assert.Equal(discount, booking.Discount);
        Assert.Equal(800_000m, booking.PaymentInfo.TotalAmount.Amount);
    }

    [Fact]
    public void The_deposit_is_taken_on_the_discounted_total()
    {
        var booking = Book(Discount(), deposit: true);

        Assert.Equal(160_000m, booking.PaymentInfo.DepositAmount.Amount); // 20% of 800,000
    }

    [Fact]
    public void The_cancellation_fee_is_taken_on_the_discounted_total()
    {
        var booking = Book(Discount());

        Assert.Equal(400_000m, booking.Policy.CalculateCancellationFee(
            Money.Create(booking.TotalPrice.Amount, booking.TotalPrice.Currency)).Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_000)]
    [InlineData(1_200_000)]
    public void A_discount_must_leave_something_to_pay(decimal amount)
    {
        Assert.Throws<DomainValidationException>(() => Book(Discount(amount)));
    }

    [Fact]
    public void A_reschedule_carries_the_discount_to_the_successor()
    {
        var discount = Discount();
        var booking = Book(discount);

        var successor = booking.Reschedule(Start.AddDays(1), booking.StaffId, "change");

        Assert.Equal(discount, successor.Discount);
        Assert.Equal(800_000m, successor.TotalPrice.Amount);
        Assert.Equal(1_000_000m, successor.SubtotalAmount);
    }

    [Fact]
    public void A_salon_entered_booking_can_be_created_without_a_discount()
    {
        var booking = Booking.CreateConfirmedByProvider(
            UserId.From(Guid.NewGuid()), ProviderId.New(), ServiceId.New(), Guid.NewGuid(),
            Start, Duration.FromMinutes(60), Price.Create(1_000_000m, "IRT"), Policy());

        Assert.Null(booking.Discount);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }
}
