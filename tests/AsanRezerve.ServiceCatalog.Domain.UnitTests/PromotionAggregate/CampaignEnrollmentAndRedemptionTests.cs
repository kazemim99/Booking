using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using static AsanRezerve.ServiceCatalog.Domain.UnitTests.PromotionAggregate.PromotionTestData;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.PromotionAggregate;

public class CampaignEnrollmentAndRedemptionTests
{
    // ── Enrollment: a salon opts into a platform campaign (decision 2026-09-25) ──

    [Fact]
    public void A_salon_joins_a_campaign()
    {
        var campaign = Campaign();
        var providerId = ProviderId.New();

        var enrollment = CampaignEnrollment.Join(campaign, providerId, Owner, Now);

        Assert.Equal(campaign.Id, enrollment.PromotionId);
        Assert.Equal(providerId, enrollment.ProviderId);
        Assert.True(enrollment.IsActive);
        Assert.Equal(Now, enrollment.JoinedAt);
        Assert.Null(enrollment.LeftAt);
    }

    [Fact]
    public void A_scheduled_campaign_can_be_joined_ahead_of_time()
    {
        var campaign = Campaign(Terms(startsAt: Now.AddDays(10)));

        Assert.True(CampaignEnrollment.Join(campaign, ProviderId.New(), Owner, Now).IsActive);
    }

    [Fact]
    public void A_salon_promotion_is_not_a_campaign_to_join()
    {
        Assert.Throws<BusinessRuleViolationException>(() =>
            CampaignEnrollment.Join(ProviderPromotion(), ProviderId.New(), Owner, Now));
    }

    [Fact]
    public void An_ended_or_expired_campaign_cannot_be_joined()
    {
        var ended = Campaign();
        ended.End(Now);
        var expiring = Campaign(Terms(endsAt: Now.AddDays(1)));

        Assert.Throws<BusinessRuleViolationException>(() =>
            CampaignEnrollment.Join(ended, ProviderId.New(), Owner, Now));
        Assert.Throws<BusinessRuleViolationException>(() =>
            CampaignEnrollment.Join(expiring, ProviderId.New(), Owner, Now.AddDays(2)));
    }

    [Fact]
    public void Leaving_and_rejoining_reuse_the_same_enrollment()
    {
        var campaign = Campaign();
        var enrollment = CampaignEnrollment.Join(campaign, ProviderId.New(), Owner, Now);

        enrollment.Leave(Now.AddHours(1));
        Assert.False(enrollment.IsActive);
        Assert.Equal(Now.AddHours(1), enrollment.LeftAt);

        enrollment.Rejoin(campaign, Owner, Now.AddHours(2));
        Assert.True(enrollment.IsActive);
        Assert.Null(enrollment.LeftAt);
        Assert.Equal(Now.AddHours(2), enrollment.JoinedAt);
    }

    [Fact]
    public void Leaving_twice_or_rejoining_while_joined_is_harmless()
    {
        var campaign = Campaign();
        var enrollment = CampaignEnrollment.Join(campaign, ProviderId.New(), Owner, Now);

        enrollment.Rejoin(campaign, Owner, Now.AddHours(1));
        Assert.Equal(Now, enrollment.JoinedAt);

        enrollment.Leave(Now.AddHours(2));
        enrollment.Leave(Now.AddHours(3));
        Assert.Equal(Now.AddHours(2), enrollment.LeftAt);
    }

    // ── Redemption: one applied discount on one booking ──

    [Fact]
    public void A_redemption_records_what_the_booking_received()
    {
        var promotion = ProviderPromotion();
        var bookingId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var redemption = PromotionRedemption.Apply(
            promotion, bookingId, customerId, promotion.ProviderId!.Value, 200_000m, "IRT", Now);

        Assert.Equal(promotion.Id, redemption.PromotionId);
        Assert.Equal(bookingId, redemption.BookingId);
        Assert.Equal(customerId, redemption.CustomerId);
        Assert.Equal(200_000m, redemption.Amount);
        Assert.Equal(PromotionRedemptionStatus.Applied, redemption.Status);
        Assert.Equal(Now, redemption.RedeemedAt);
    }

    [Fact]
    public void A_redemption_must_carry_a_positive_amount()
    {
        var promotion = ProviderPromotion();

        Assert.Throws<DomainValidationException>(() =>
            PromotionRedemption.Apply(promotion, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, "IRT", Now));
    }

    [Fact]
    public void Releasing_is_idempotent()
    {
        var redemption = PromotionRedemption.Apply(
            ProviderPromotion(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1_000m, "IRT", Now);

        Assert.True(redemption.Release(Now.AddHours(1)));
        Assert.False(redemption.Release(Now.AddHours(2)));

        Assert.Equal(PromotionRedemptionStatus.Released, redemption.Status);
        Assert.Equal(Now.AddHours(1), redemption.ReleasedAt);
    }

    [Fact]
    public void A_redemption_follows_its_booking_to_the_rescheduled_successor()
    {
        var redemption = PromotionRedemption.Apply(
            ProviderPromotion(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1_000m, "IRT", Now);
        var successor = Guid.NewGuid();

        redemption.TransferTo(successor);

        Assert.Equal(successor, redemption.BookingId);
        Assert.Equal(PromotionRedemptionStatus.Applied, redemption.Status);
    }
}
