using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using DayOfWeek = AsanRezerve.ServiceCatalog.Domain.Enums.DayOfWeek;
using static AsanRezerve.ServiceCatalog.Domain.UnitTests.PromotionAggregate.PromotionTestData;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.PromotionAggregate;

/// <summary>
/// Whether one promotion applies to one visit, and how much it takes off. Each condition is proven on its own,
/// with the reason the customer is shown when it fails.
/// </summary>
public class PromotionEligibilityTests
{
    [Fact]
    public void A_plain_percentage_applies_to_the_whole_visit()
    {
        var evaluation = ProviderPromotion(Terms(value: 20)).Evaluate(Context(1_000_000m), customerPriorUses: 0);

        Assert.True(evaluation.IsEligible);
        Assert.Equal(200_000m, evaluation.Discount);
        Assert.Equal(1_000_000m, evaluation.EligibleSubtotal);
        Assert.Null(evaluation.Reason);
    }

    [Fact]
    public void A_percentage_is_capped_by_its_maximum()
    {
        var evaluation = ProviderPromotion(Terms(value: 30, max: 100_000)).Evaluate(Context(1_000_000m), 0);

        Assert.Equal(100_000m, evaluation.Discount);
    }

    [Fact]
    public void A_fixed_amount_is_taken_off_as_is()
    {
        var evaluation = ProviderPromotion(Terms(kind: DiscountKind.FixedAmount, value: 50_000))
            .Evaluate(Context(300_000m), 0);

        Assert.Equal(50_000m, evaluation.Discount);
    }

    [Fact]
    public void No_discount_exceeds_90_percent_of_what_it_applies_to()
    {
        var evaluation = ProviderPromotion(Terms(kind: DiscountKind.FixedAmount, value: 500_000))
            .Evaluate(Context(300_000m), 0);

        Assert.Equal(270_000m, evaluation.Discount);
    }

    [Fact]
    public void The_discount_is_rounded_down_to_a_whole_Toman()
    {
        // 15% of 33,333 = 4,999.95
        var evaluation = ProviderPromotion(Terms(value: 15)).Evaluate(Context(33_333m), 0);

        Assert.Equal(4_999m, evaluation.Discount);
    }

    [Fact]
    public void Targeted_services_limit_the_discount_to_their_lines()
    {
        var haircut = Guid.NewGuid();
        var colour = Guid.NewGuid();
        var promotion = ProviderPromotion(Terms(value: 50, serviceIds: new[] { haircut }));

        var evaluation = promotion.Evaluate(
            Context(lines: new[] { new PricedLine(haircut, 200_000m), new PricedLine(colour, 800_000m) }), 0);

        Assert.Equal(200_000m, evaluation.EligibleSubtotal);
        Assert.Equal(100_000m, evaluation.Discount);
    }

    [Fact]
    public void A_visit_without_any_targeted_service_is_not_eligible()
    {
        var promotion = ProviderPromotion(Terms(serviceIds: new[] { Guid.NewGuid() }));

        var evaluation = promotion.Evaluate(Context(), 0);

        Assert.False(evaluation.IsEligible);
        Assert.Equal(0m, evaluation.Discount);
        Assert.Equal("این تخفیف شامل خدمات انتخاب‌شده نمی‌شود.", evaluation.Reason);
    }

    [Fact]
    public void Before_its_start_it_does_not_apply()
    {
        var evaluation = ProviderPromotion(Terms(startsAt: Now.AddDays(1))).Evaluate(Context(), 0);

        Assert.False(evaluation.IsEligible);
        Assert.Equal("این تخفیف هنوز شروع نشده است.", evaluation.Reason);
    }

    [Fact]
    public void After_its_end_it_does_not_apply()
    {
        var promotion = ProviderPromotion(Terms(endsAt: Now.AddDays(1)));

        var evaluation = promotion.Evaluate(Context(now: Now.AddDays(2)), 0);

        Assert.False(evaluation.IsEligible);
        Assert.Equal("مهلت این تخفیف تمام شده است.", evaluation.Reason);
    }

    [Fact]
    public void Paused_and_ended_promotions_do_not_apply()
    {
        var paused = ProviderPromotion();
        paused.Pause(false, Now);
        var ended = ProviderPromotion();
        ended.End(Now);

        Assert.False(paused.Evaluate(Context(), 0).IsEligible);
        Assert.False(ended.Evaluate(Context(), 0).IsEligible);
        Assert.Equal("این تخفیف در حال حاضر فعال نیست.", paused.Evaluate(Context(), 0).Reason);
    }

    [Fact]
    public void Days_of_week_are_read_from_the_appointment_at_the_salon()
    {
        var promotion = ProviderPromotion(Terms(days: new[]
        {
            DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday
        }));

        Assert.True(promotion.Evaluate(Context(appointment: SaturdayEleven), 0).IsEligible);

        var thursday = promotion.Evaluate(Context(appointment: ThursdayEleven), 0);
        Assert.False(thursday.IsEligible);
        Assert.Equal("این تخفیف در روز انتخاب‌شده معتبر نیست.", thursday.Reason);
    }

    [Theory]
    [InlineData(10, 0, true)]   // the window's first minute
    [InlineData(13, 30, true)]  // starts inside
    [InlineData(14, 0, false)]  // the window's end is exclusive
    [InlineData(9, 59, false)]
    public void The_daily_window_is_read_from_the_appointment_start(int hour, int minute, bool eligible)
    {
        var promotion = ProviderPromotion(Terms(dailyStart: new TimeOnly(10, 0), dailyEnd: new TimeOnly(14, 0)));
        var appointment = SaturdayEleven.Date.AddHours(hour).AddMinutes(minute);

        var evaluation = promotion.Evaluate(Context(appointment: appointment), 0);

        Assert.Equal(eligible, evaluation.IsEligible);
        if (!eligible)
            Assert.Equal("این تخفیف فقط برای نوبت‌های ساعت 10:00 تا 14:00 است.", evaluation.Reason);
    }

    [Fact]
    public void A_minimum_subtotal_must_be_met()
    {
        var promotion = ProviderPromotion(Terms(minimumSubtotal: 500_000));

        Assert.True(promotion.Evaluate(Context(500_000m), 0).IsEligible);

        var below = promotion.Evaluate(Context(499_999m), 0);
        Assert.False(below.IsEligible);
        Assert.Equal("حداقل مبلغ برای این تخفیف 500,000 تومان است.", below.Reason);
    }

    [Fact]
    public void New_customers_only_means_new_to_this_salon()
    {
        var promotion = ProviderPromotion(Terms(newCustomersOnly: true));

        Assert.True(promotion.Evaluate(Context(newCustomer: true), 0).IsEligible);

        var returning = promotion.Evaluate(Context(newCustomer: false), 0);
        Assert.False(returning.IsEligible);
        Assert.Equal("این تخفیف فقط برای اولین نوبت در این سالن است.", returning.Reason);
    }

    [Fact]
    public void The_per_customer_limit_counts_the_customers_earlier_uses()
    {
        var promotion = ProviderPromotion(Terms(perCustomerLimit: 2));

        Assert.True(promotion.Evaluate(Context(), customerPriorUses: 1).IsEligible);

        var spent = promotion.Evaluate(Context(), customerPriorUses: 2);
        Assert.False(spent.IsEligible);
        Assert.Equal("شما قبلاً از این تخفیف استفاده کرده‌اید.", spent.Reason);
    }

    [Fact]
    public void An_exhausted_promotion_does_not_apply()
    {
        var promotion = ProviderPromotion(Terms(totalLimit: 1));
        promotion.RecordRedemption(0, Now);

        var evaluation = promotion.Evaluate(Context(), 0);

        Assert.False(evaluation.IsEligible);
        Assert.Equal("ظرفیت استفاده از این تخفیف تکمیل شده است.", evaluation.Reason);
    }

    [Fact]
    public void A_visit_too_cheap_to_discount_is_not_eligible()
    {
        // 1% of 50 Toman floors to zero: nothing to take off.
        var evaluation = ProviderPromotion(Terms(value: 1)).Evaluate(Context(50m), 0);

        Assert.False(evaluation.IsEligible);
    }

    // ── Redemption counting ──

    [Fact]
    public void Recording_and_releasing_a_redemption_moves_the_count()
    {
        var promotion = ProviderPromotion(Terms(totalLimit: 2));

        promotion.RecordRedemption(0, Now);
        promotion.RecordRedemption(0, Now);
        Assert.Equal(2, promotion.RedemptionCount);

        promotion.ReleaseRedemption();
        Assert.Equal(1, promotion.RedemptionCount);
        Assert.Equal(PromotionState.Active, promotion.StateAt(Now));
    }

    [Fact]
    public void Recording_beyond_a_limit_is_refused()
    {
        var total = ProviderPromotion(Terms(totalLimit: 1));
        total.RecordRedemption(0, Now);
        Assert.Throws<BusinessRuleViolationException>(() => total.RecordRedemption(0, Now));

        var perCustomer = ProviderPromotion(Terms(perCustomerLimit: 1));
        Assert.Throws<BusinessRuleViolationException>(() => perCustomer.RecordRedemption(customerPriorUses: 1, Now));
    }

    [Fact]
    public void Recording_on_an_inactive_promotion_is_refused()
    {
        var promotion = ProviderPromotion();
        promotion.Pause(false, Now);

        Assert.Throws<BusinessRuleViolationException>(() => promotion.RecordRedemption(0, Now));
    }

    [Fact]
    public void Releasing_never_drives_the_count_below_zero()
    {
        var promotion = ProviderPromotion();

        promotion.ReleaseRedemption();

        Assert.Equal(0, promotion.RedemptionCount);
    }

    [Fact]
    public void A_release_is_accepted_even_after_the_promotion_ended()
    {
        // The customer's cancellation must not fail because the salon ended the offer in between.
        var promotion = ProviderPromotion();
        promotion.RecordRedemption(0, Now);
        promotion.End(Now);

        promotion.ReleaseRedemption();

        Assert.Equal(0, promotion.RedemptionCount);
    }
}
