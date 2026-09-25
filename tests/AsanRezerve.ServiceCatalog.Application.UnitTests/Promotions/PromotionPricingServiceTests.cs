using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Application.Promotions;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using NSubstitute;

namespace AsanRezerve.ServiceCatalog.Application.UnitTests.Promotions;

/// <summary>
/// The application half of pricing: which promotions are candidates at a salon, what is known about the customer,
/// and keeping redemptions in step with bookings. The choice itself is <c>PromotionPricing</c>'s, tested in Domain.
/// </summary>
public class PromotionPricingServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 6, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime Appointment = new(2026, 10, 3, 11, 0, 0);

    private readonly IPromotionRepository _promotions = Substitute.For<IPromotionRepository>();
    private readonly IPromotionRedemptionRepository _redemptions = Substitute.For<IPromotionRedemptionRepository>();
    private readonly ProviderId _provider = ProviderId.New();
    private readonly Guid _customer = Guid.NewGuid();

    private PromotionPricingService Service => new(_promotions, _redemptions);

    private static PromotionTerms Terms(
        decimal value = 20m, bool newCustomersOnly = false, int? perCustomerLimit = null, int? totalLimit = null,
        PromotionActivation activation = PromotionActivation.Automatic, string? code = null) =>
        new("تخفیف", null, activation, code, DiscountKind.Percentage, value, null, null, newCustomersOnly,
            null, null, null, null, Now.AddDays(-1), null, totalLimit, perCustomerLimit);

    private Promotion SalonPromotion(PromotionTerms terms) =>
        Promotion.CreateForProvider(_provider, terms, Guid.NewGuid(), Now);

    private void Candidates(params PromotionWithEnrollment[] candidates) =>
        _promotions.GetPricingCandidatesAsync(_provider, Now, Arg.Any<CancellationToken>()).Returns(candidates);

    private PricingRequest Request(string? code = null) =>
        new(_provider, new[] { new PricedLine(Guid.NewGuid(), 1_000_000m) }, "IRT", Appointment, _customer, code, Now);

    [Fact]
    public async Task Prices_with_the_salons_promotions_and_the_campaigns_it_joined()
    {
        var own = SalonPromotion(Terms(10));
        var joined = Promotion.CreatePlatformCampaign(Terms(15), Guid.NewGuid(), Now);
        var notJoined = Promotion.CreatePlatformCampaign(Terms(40), Guid.NewGuid(), Now);
        Candidates(new PromotionWithEnrollment(own, true), new PromotionWithEnrollment(joined, true), new PromotionWithEnrollment(notJoined, false));

        var result = await Service.PriceAsync(Request());

        Assert.Equal(joined.Id, result.Quote.Applied!.PromotionId);
        Assert.Equal(150_000m, result.Quote.Discount);
        Assert.Equal(850_000m, result.Quote.Total);
    }

    [Fact]
    public async Task A_returning_customer_does_not_get_a_new_customers_only_promotion()
    {
        Candidates(new PromotionWithEnrollment(SalonPromotion(Terms(newCustomersOnly: true)), true));
        _promotions.HasPriorBookingAsync(_customer, _provider, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Service.PriceAsync(Request());

        Assert.Null(result.Quote.Applied);
    }

    [Fact]
    public async Task A_new_customer_gets_a_new_customers_only_promotion()
    {
        Candidates(new PromotionWithEnrollment(SalonPromotion(Terms(newCustomersOnly: true)), true));
        _promotions.HasPriorBookingAsync(_customer, _provider, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Service.PriceAsync(Request());

        Assert.NotNull(result.Quote.Applied);
    }

    [Fact]
    public async Task Booking_history_is_not_read_when_no_promotion_needs_it()
    {
        Candidates(new PromotionWithEnrollment(SalonPromotion(Terms()), true));

        await Service.PriceAsync(Request());

        await _promotions.DidNotReceive().HasPriorBookingAsync(
            Arg.Any<Guid>(), Arg.Any<ProviderId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_customers_earlier_uses_count_against_the_per_customer_limit()
    {
        var once = SalonPromotion(Terms(30, perCustomerLimit: 1));
        Candidates(new PromotionWithEnrollment(once, true));
        _redemptions.AppliedCountsForCustomerAsync(_customer, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [once.Id] = 1 });

        var result = await Service.PriceAsync(Request());

        Assert.Null(result.Quote.Applied);
    }

    [Fact]
    public async Task Redeeming_counts_the_use_and_records_it_against_the_booking()
    {
        var promotion = SalonPromotion(Terms(20, totalLimit: 5));
        Candidates(new PromotionWithEnrollment(promotion, true));
        _promotions.GetAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);
        var bookingId = Guid.NewGuid();
        var pricing = await Service.PriceAsync(Request());

        var applied = await Service.RedeemAsync(pricing, bookingId);

        Assert.Equal(200_000m, applied!.Amount);
        Assert.Equal(1, promotion.RedemptionCount);
        await _redemptions.Received(1).AddAsync(
            Arg.Is<PromotionRedemption>(r =>
                r.BookingId == bookingId && r.PromotionId == promotion.Id && r.CustomerId == _customer
                && r.Amount == 200_000m && r.ProviderId == _provider.Value),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Nothing_is_redeemed_when_no_promotion_applied()
    {
        Candidates();
        var pricing = await Service.PriceAsync(Request());

        var applied = await Service.RedeemAsync(pricing, Guid.NewGuid());

        Assert.Null(applied);
        await _redemptions.DidNotReceive().AddAsync(Arg.Any<PromotionRedemption>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_promotion_that_vanished_between_pricing_and_redeeming_is_a_conflict()
    {
        var promotion = SalonPromotion(Terms());
        Candidates(new PromotionWithEnrollment(promotion, true));
        var pricing = await Service.PriceAsync(Request());

        await Assert.ThrowsAsync<ConflictException>(() => Service.RedeemAsync(pricing, Guid.NewGuid()));
    }

    [Fact]
    public async Task A_limit_reached_between_pricing_and_redeeming_is_a_conflict_to_retry()
    {
        var priced = SalonPromotion(Terms(totalLimit: 1));
        Candidates(new PromotionWithEnrollment(priced, true));
        var pricing = await Service.PriceAsync(Request());

        // Someone else took the last use in between.
        var current = SalonPromotion(Terms(totalLimit: 1));
        current.RecordRedemption(0, Now);
        _promotions.GetAsync(priced.Id, Arg.Any<CancellationToken>()).Returns(current);

        await Assert.ThrowsAsync<ConflictException>(() => Service.RedeemAsync(pricing, Guid.NewGuid()));
    }

    [Fact]
    public async Task Cancelling_releases_the_use()
    {
        var promotion = SalonPromotion(Terms());
        promotion.RecordRedemption(0, Now);
        var bookingId = Guid.NewGuid();
        var redemption = PromotionRedemption.Apply(promotion, bookingId, _customer, _provider.Value, 1_000m, "IRT", Now);
        _redemptions.GetAppliedForBookingAsync(bookingId, Arg.Any<CancellationToken>()).Returns(redemption);
        _promotions.GetAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        await Service.ReleaseForBookingAsync(bookingId, Now);

        Assert.Equal(PromotionRedemptionStatus.Released, redemption.Status);
        Assert.Equal(0, promotion.RedemptionCount);
    }

    [Fact]
    public async Task Cancelling_a_booking_without_a_discount_changes_nothing()
    {
        await Service.ReleaseForBookingAsync(Guid.NewGuid(), Now);

        await _promotions.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_reschedule_moves_the_redemption_to_the_successor()
    {
        var promotion = SalonPromotion(Terms());
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        var redemption = PromotionRedemption.Apply(promotion, from, _customer, _provider.Value, 1_000m, "IRT", Now);
        _redemptions.GetAppliedForBookingAsync(from, Arg.Any<CancellationToken>()).Returns(redemption);

        await Service.TransferAsync(from, to);

        Assert.Equal(to, redemption.BookingId);
    }
}
