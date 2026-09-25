using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using static AsanRezerve.ServiceCatalog.Domain.UnitTests.PromotionAggregate.PromotionTestData;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.PromotionAggregate;

/// <summary>
/// Choosing the one discount a visit gets (decision 2026-09-25: at most one, the best), and what the customer is
/// told about the code they typed.
/// </summary>
public class PromotionPricingTests
{
    private static PromotionCandidate Candidate(Promotion p, int priorUses = 0, bool enrolled = true) =>
        new(p, priorUses, enrolled);

    private static PriceQuote Quote(string? code, params PromotionCandidate[] candidates) =>
        PromotionPricing.Quote(Context(1_000_000m), "IRT", candidates, code);

    [Fact]
    public void Without_promotions_the_total_is_the_subtotal()
    {
        var quote = Quote(null);

        Assert.Equal(1_000_000m, quote.Subtotal);
        Assert.Equal(0m, quote.Discount);
        Assert.Equal(1_000_000m, quote.Total);
        Assert.Null(quote.Applied);
        Assert.Equal(PromotionCodeOutcome.None, quote.CodeOutcome);
        Assert.Null(quote.CodeMessage);
        Assert.Equal("IRT", quote.Currency);
    }

    [Fact]
    public void The_largest_automatic_discount_wins()
    {
        var ten = ProviderPromotion(Terms(title: "ده", value: 10));
        var twenty = ProviderPromotion(Terms(title: "بیست", value: 20));

        var quote = Quote(null, Candidate(ten), Candidate(twenty));

        Assert.Equal(200_000m, quote.Discount);
        Assert.Equal(800_000m, quote.Total);
        Assert.Equal(twenty.Id, quote.Applied!.PromotionId);
        Assert.Equal("بیست", quote.Applied.Title);
        Assert.Equal(PromotionOwner.Provider, quote.Applied.Owner);
        Assert.Equal(200_000m, quote.Applied.Amount);
    }

    [Fact]
    public void Ineligible_automatic_promotions_are_skipped()
    {
        var big = ProviderPromotion(Terms(value: 50, newCustomersOnly: true));
        var small = ProviderPromotion(Terms(value: 5));

        var quote = Quote(null, Candidate(big), Candidate(small));

        Assert.Equal(small.Id, quote.Applied!.PromotionId);
    }

    [Fact]
    public void Code_promotions_never_apply_on_their_own()
    {
        var coded = ProviderPromotion(Terms(activation: PromotionActivation.Code, code: "SECRET", value: 30));

        var quote = Quote(null, Candidate(coded));

        Assert.Null(quote.Applied);
        Assert.Equal(0m, quote.Discount);
    }

    [Fact]
    public void A_matching_code_is_applied_case_insensitively()
    {
        var coded = ProviderPromotion(Terms(activation: PromotionActivation.Code, code: "SECRET", value: 30));

        var quote = Quote(" secret ", Candidate(coded));

        Assert.Equal(PromotionCodeOutcome.Applied, quote.CodeOutcome);
        Assert.Equal("کد تخفیف اعمال شد.", quote.CodeMessage);
        Assert.Equal(300_000m, quote.Discount);
        Assert.Equal("SECRET", quote.Applied!.Code);
    }

    [Fact]
    public void An_unknown_code_is_reported_and_the_automatic_offer_still_applies()
    {
        var auto = ProviderPromotion(Terms(value: 10));

        var quote = Quote("NOPE", Candidate(auto));

        Assert.Equal(PromotionCodeOutcome.NotFound, quote.CodeOutcome);
        Assert.Equal("کد تخفیف معتبر نیست.", quote.CodeMessage);
        Assert.Equal(auto.Id, quote.Applied!.PromotionId);
    }

    [Fact]
    public void An_ineligible_code_reports_why()
    {
        var coded = ProviderPromotion(Terms(activation: PromotionActivation.Code, code: "FIRST", newCustomersOnly: true));

        var quote = Quote("FIRST", Candidate(coded));

        Assert.Equal(PromotionCodeOutcome.NotEligible, quote.CodeOutcome);
        Assert.Equal("این تخفیف فقط برای اولین نوبت در این سالن است.", quote.CodeMessage);
        Assert.Null(quote.Applied);
    }

    [Fact]
    public void A_code_replaces_the_automatic_offer_only_when_larger()
    {
        var auto = ProviderPromotion(Terms(value: 20));
        var smaller = ProviderPromotion(Terms(activation: PromotionActivation.Code, code: "SMALL", value: 10));
        var equal = ProviderPromotion(Terms(activation: PromotionActivation.Code, code: "EQUAL", value: 20));
        var larger = ProviderPromotion(Terms(activation: PromotionActivation.Code, code: "LARGE", value: 25));
        var all = new[] { Candidate(auto), Candidate(smaller), Candidate(equal), Candidate(larger) };

        var withSmaller = PromotionPricing.Quote(Context(), "IRT", all, "SMALL");
        Assert.Equal(PromotionCodeOutcome.BetterOfferApplied, withSmaller.CodeOutcome);
        Assert.Equal("تخفیف بهتری روی این نوبت اعمال شده است.", withSmaller.CodeMessage);
        Assert.Equal(auto.Id, withSmaller.Applied!.PromotionId);

        var withEqual = PromotionPricing.Quote(Context(), "IRT", all, "EQUAL");
        Assert.Equal(PromotionCodeOutcome.BetterOfferApplied, withEqual.CodeOutcome);
        Assert.Equal(auto.Id, withEqual.Applied!.PromotionId);

        var withLarger = PromotionPricing.Quote(Context(), "IRT", all, "LARGE");
        Assert.Equal(PromotionCodeOutcome.Applied, withLarger.CodeOutcome);
        Assert.Equal(larger.Id, withLarger.Applied!.PromotionId);
        Assert.Equal(250_000m, withLarger.Discount);
    }

    [Fact]
    public void When_a_salon_code_and_a_campaign_code_collide_the_larger_applies()
    {
        var salon = ProviderPromotion(Terms(activation: PromotionActivation.Code, code: "NOWRUZ", value: 10));
        var campaign = Campaign(Terms(activation: PromotionActivation.Code, code: "NOWRUZ", value: 15));

        var quote = Quote("NOWRUZ", Candidate(salon), Candidate(campaign));

        Assert.Equal(campaign.Id, quote.Applied!.PromotionId);
        Assert.Equal(PromotionOwner.Platform, quote.Applied.Owner);
    }

    [Fact]
    public void A_campaign_the_salon_has_not_joined_never_applies()
    {
        var campaign = Campaign(Terms(value: 30));

        var quote = Quote(null, Candidate(campaign, enrolled: false));

        Assert.Null(quote.Applied);
    }

    [Fact]
    public void A_campaign_code_at_a_salon_that_has_not_joined_says_so()
    {
        var campaign = Campaign(Terms(activation: PromotionActivation.Code, code: "YALDA", value: 30));

        var quote = Quote("YALDA", Candidate(campaign, enrolled: false));

        Assert.Equal(PromotionCodeOutcome.NotEligible, quote.CodeOutcome);
        Assert.Equal("این کمپین در این سالن فعال نیست.", quote.CodeMessage);
    }

    [Fact]
    public void Ties_prefer_the_salons_own_promotion_then_the_one_ending_first()
    {
        var campaign = Campaign(Terms(value: 20));
        var salonLate = ProviderPromotion(Terms(value: 20, endsAt: Now.AddDays(30)));
        var salonSoon = ProviderPromotion(Terms(value: 20, endsAt: Now.AddDays(3)));

        var quote = Quote(null, Candidate(campaign), Candidate(salonLate), Candidate(salonSoon));

        Assert.Equal(salonSoon.Id, quote.Applied!.PromotionId);
    }

    [Fact]
    public void The_per_customer_uses_are_taken_per_candidate()
    {
        var once = ProviderPromotion(Terms(value: 30, perCustomerLimit: 1));
        var fallback = ProviderPromotion(Terms(value: 10));

        var quote = Quote(null, Candidate(once, priorUses: 1), Candidate(fallback));

        Assert.Equal(fallback.Id, quote.Applied!.PromotionId);
    }

    [Fact]
    public void Evaluating_never_changes_a_promotion()
    {
        var promotion = ProviderPromotion(Terms(totalLimit: 1));

        Quote(null, Candidate(promotion));

        Assert.Equal(0, promotion.RedemptionCount);
    }
}
