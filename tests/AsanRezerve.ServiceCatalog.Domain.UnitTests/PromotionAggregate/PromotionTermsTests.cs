using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using DayOfWeek = AsanRezerve.ServiceCatalog.Domain.Enums.DayOfWeek;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using static AsanRezerve.ServiceCatalog.Domain.UnitTests.PromotionAggregate.PromotionTestData;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.PromotionAggregate;

/// <summary>
/// What a promotion may say. Everything a salon or an admin can type is checked here, by name, in Persian —
/// the API's validators mirror these rules but the aggregate is the last word.
/// </summary>
public class PromotionTermsTests
{
    [Fact]
    public void A_valid_provider_promotion_is_active_and_belongs_to_its_salon()
    {
        var providerId = ProviderId.New();

        var promotion = Promotion.CreateForProvider(providerId, Terms(), Owner, Now);

        Assert.Equal(PromotionOwner.Provider, promotion.Owner);
        Assert.Equal(providerId, promotion.ProviderId);
        Assert.Equal(PromotionStatus.Active, promotion.Status);
        Assert.Equal(PromotionState.Active, promotion.StateAt(Now));
        Assert.Equal(0, promotion.RedemptionCount);
        Assert.Equal(Owner, promotion.CreatedBy);
        Assert.Equal("IRT", promotion.Currency);
    }

    [Fact]
    public void A_platform_campaign_has_no_salon()
    {
        var campaign = Campaign();

        Assert.Equal(PromotionOwner.Platform, campaign.Owner);
        Assert.Null(campaign.ProviderId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(90.01)]
    [InlineData(100)]
    public void A_percentage_outside_1_to_90_is_refused(decimal value)
    {
        var ex = Assert.Throws<DomainValidationException>(() => ProviderPromotion(Terms(value: value)));
        Assert.Contains("DiscountValue", ex.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(90)]
    [InlineData(12.5)]
    public void A_percentage_from_1_to_90_is_accepted(decimal value)
    {
        Assert.Equal(value, ProviderPromotion(Terms(value: value)).DiscountValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_fixed_amount_must_be_positive(decimal value)
    {
        Assert.Throws<DomainValidationException>(() =>
            ProviderPromotion(Terms(kind: DiscountKind.FixedAmount, value: value)));
    }

    [Fact]
    public void A_fixed_amount_that_is_not_whole_Toman_is_refused()
    {
        Assert.Throws<DomainValidationException>(() =>
            ProviderPromotion(Terms(kind: DiscountKind.FixedAmount, value: 1000.5m)));
    }

    [Fact]
    public void A_cap_is_only_for_percentages_and_must_be_positive()
    {
        Assert.Throws<DomainValidationException>(() =>
            ProviderPromotion(Terms(kind: DiscountKind.FixedAmount, value: 50_000, max: 10_000)));
        Assert.Throws<DomainValidationException>(() => ProviderPromotion(Terms(max: 0)));
        Assert.Equal(100_000m, ProviderPromotion(Terms(max: 100_000)).MaxDiscountAmount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_title_is_required(string title)
    {
        Assert.Throws<DomainValidationException>(() => ProviderPromotion(Terms(title: title)));
    }

    [Fact]
    public void A_title_longer_than_80_is_refused()
    {
        Assert.Throws<DomainValidationException>(() => ProviderPromotion(Terms(title: new string('ت', 81))));
    }

    [Fact]
    public void Title_and_description_are_trimmed()
    {
        var promotion = ProviderPromotion(Terms(title: "  یلدا  ", description: "  شب یلدا  "));

        Assert.Equal("یلدا", promotion.Title);
        Assert.Equal("شب یلدا", promotion.Description);
    }

    [Fact]
    public void A_code_promotion_needs_a_code()
    {
        Assert.Throws<DomainValidationException>(() =>
            ProviderPromotion(Terms(activation: PromotionActivation.Code, code: null)));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("TOO-LONG-CODE-123456789")]
    [InlineData("نوروز")]
    [InlineData("SPACE CODE")]
    public void A_malformed_code_is_refused(string code)
    {
        Assert.Throws<DomainValidationException>(() =>
            ProviderPromotion(Terms(activation: PromotionActivation.Code, code: code)));
    }

    [Fact]
    public void A_code_is_stored_upper_case_and_trimmed()
    {
        var promotion = ProviderPromotion(Terms(activation: PromotionActivation.Code, code: "  yalda-1405 "));

        Assert.Equal("YALDA-1405", promotion.Code);
    }

    [Fact]
    public void An_automatic_promotion_keeps_no_code()
    {
        var promotion = ProviderPromotion(Terms(activation: PromotionActivation.Automatic, code: "IGNORED"));

        Assert.Null(promotion.Code);
    }

    [Fact]
    public void The_end_must_be_after_the_start()
    {
        Assert.Throws<DomainValidationException>(() =>
            ProviderPromotion(Terms(startsAt: Now.AddDays(2), endsAt: Now.AddDays(1))));
    }

    [Fact]
    public void A_promotion_that_has_already_ended_cannot_be_created()
    {
        Assert.Throws<DomainValidationException>(() =>
            ProviderPromotion(Terms(startsAt: Now.AddDays(-10), endsAt: Now.AddDays(-1))));
    }

    [Fact]
    public void A_daily_window_needs_both_ends_and_a_start_before_its_end()
    {
        Assert.Throws<DomainValidationException>(() => ProviderPromotion(Terms(dailyStart: new TimeOnly(10, 0))));
        Assert.Throws<DomainValidationException>(() =>
            ProviderPromotion(Terms(dailyStart: new TimeOnly(14, 0), dailyEnd: new TimeOnly(10, 0))));
        Assert.Throws<DomainValidationException>(() =>
            ProviderPromotion(Terms(dailyStart: new TimeOnly(10, 0), dailyEnd: new TimeOnly(10, 0))));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Usage_limits_must_be_at_least_one(int limit)
    {
        Assert.Throws<DomainValidationException>(() => ProviderPromotion(Terms(totalLimit: limit)));
        Assert.Throws<DomainValidationException>(() => ProviderPromotion(Terms(perCustomerLimit: limit)));
    }

    [Fact]
    public void A_negative_minimum_subtotal_is_refused()
    {
        Assert.Throws<DomainValidationException>(() => ProviderPromotion(Terms(minimumSubtotal: -1)));
    }

    [Fact]
    public void A_platform_campaign_cannot_target_services()
    {
        Assert.Throws<DomainValidationException>(() => Campaign(Terms(serviceIds: new[] { Guid.NewGuid() })));
    }

    [Fact]
    public void Targeted_services_are_deduplicated()
    {
        var id = Guid.NewGuid();

        var promotion = ProviderPromotion(Terms(serviceIds: new[] { id, id }));

        Assert.Equal(new[] { id }, promotion.ServiceIds);
    }

    [Fact]
    public void Days_of_week_are_kept_as_given_and_empty_means_every_day()
    {
        var everyDay = ProviderPromotion(Terms());
        var weekdays = ProviderPromotion(Terms(days: new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }));

        Assert.Empty(everyDay.DaysOfWeek);
        Assert.Equal(new[] { DayOfWeek.Sunday, DayOfWeek.Saturday }, weekdays.DaysOfWeek.OrderBy(d => (int)d));
    }

    [Fact]
    public void Updating_replaces_the_terms()
    {
        var promotion = ProviderPromotion();

        promotion.Update(Terms(title: "جدید", value: 10), Now);

        Assert.Equal("جدید", promotion.Title);
        Assert.Equal(10m, promotion.DiscountValue);
        Assert.NotNull(promotion.UpdatedAt);
    }

    [Fact]
    public void The_code_cannot_change_once_the_promotion_has_been_used()
    {
        var promotion = ProviderPromotion(Terms(activation: PromotionActivation.Code, code: "FIRST"));
        promotion.RecordRedemption(customerPriorUses: 0, Now);

        Assert.Throws<BusinessRuleViolationException>(() =>
            promotion.Update(Terms(activation: PromotionActivation.Code, code: "SECOND"), Now));

        // Everything else may still change.
        promotion.Update(Terms(activation: PromotionActivation.Code, code: "first", value: 15), Now);
        Assert.Equal(15m, promotion.DiscountValue);
    }

    [Fact]
    public void An_ended_promotion_cannot_be_edited()
    {
        var promotion = ProviderPromotion();
        promotion.End(Now);

        Assert.Throws<BusinessRuleViolationException>(() => promotion.Update(Terms(), Now));
    }
}
