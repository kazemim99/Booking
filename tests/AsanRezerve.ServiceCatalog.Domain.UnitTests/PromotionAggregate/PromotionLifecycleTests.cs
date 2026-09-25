using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using static AsanRezerve.ServiceCatalog.Domain.UnitTests.PromotionAggregate.PromotionTestData;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.PromotionAggregate;

/// <summary>
/// Stored status is what someone decided (active, paused, ended); the state customers meet is derived from it
/// plus the dates and the usage, because "expired" and "exhausted" happen without anyone doing anything.
/// </summary>
public class PromotionLifecycleTests
{
    [Fact]
    public void Before_its_start_a_promotion_is_scheduled()
    {
        var promotion = ProviderPromotion(Terms(startsAt: Now.AddDays(3)));

        Assert.Equal(PromotionState.Scheduled, promotion.StateAt(Now));
        Assert.Equal(PromotionState.Active, promotion.StateAt(Now.AddDays(3)));
    }

    [Fact]
    public void After_its_end_a_promotion_is_expired()
    {
        var promotion = ProviderPromotion(Terms(endsAt: Now.AddDays(1)));

        Assert.Equal(PromotionState.Active, promotion.StateAt(Now));
        Assert.Equal(PromotionState.Expired, promotion.StateAt(Now.AddDays(1)));
    }

    [Fact]
    public void A_promotion_whose_uses_are_spent_is_exhausted()
    {
        var promotion = ProviderPromotion(Terms(totalLimit: 1));

        promotion.RecordRedemption(0, Now);

        Assert.Equal(PromotionState.Exhausted, promotion.StateAt(Now));
    }

    [Fact]
    public void Pause_and_resume()
    {
        var promotion = ProviderPromotion();

        promotion.Pause(byPlatform: false, Now);
        Assert.Equal(PromotionState.Paused, promotion.StateAt(Now));

        promotion.Resume(byPlatform: false, Now);
        Assert.Equal(PromotionState.Active, promotion.StateAt(Now));
    }

    [Fact]
    public void Pausing_a_paused_promotion_or_resuming_an_active_one_is_refused()
    {
        var promotion = ProviderPromotion();

        Assert.Throws<BusinessRuleViolationException>(() => promotion.Resume(false, Now));
        promotion.Pause(false, Now);
        Assert.Throws<BusinessRuleViolationException>(() => promotion.Pause(false, Now));
    }

    [Fact]
    public void A_salon_cannot_resume_what_the_platform_paused()
    {
        var promotion = ProviderPromotion();
        promotion.Pause(byPlatform: true, Now);

        Assert.True(promotion.PausedByPlatform);
        Assert.Throws<BusinessRuleViolationException>(() => promotion.Resume(byPlatform: false, Now));

        promotion.Resume(byPlatform: true, Now);
        Assert.False(promotion.PausedByPlatform);
        Assert.Equal(PromotionState.Active, promotion.StateAt(Now));
    }

    [Fact]
    public void Ending_is_terminal()
    {
        var promotion = ProviderPromotion();

        promotion.End(Now);

        Assert.Equal(PromotionState.Ended, promotion.StateAt(Now));
        Assert.Equal(Now, promotion.EndedAt);
        Assert.Throws<BusinessRuleViolationException>(() => promotion.Resume(true, Now));
        Assert.Throws<BusinessRuleViolationException>(() => promotion.Pause(true, Now));
        Assert.Throws<BusinessRuleViolationException>(() => promotion.End(Now));
    }

    [Fact]
    public void An_ended_promotion_reads_as_ended_even_if_it_was_also_expired()
    {
        var promotion = ProviderPromotion(Terms(endsAt: Now.AddDays(1)));
        promotion.End(Now);

        Assert.Equal(PromotionState.Ended, promotion.StateAt(Now.AddDays(5)));
    }
}
