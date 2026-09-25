using AsanRezerve.Core.Domain.Exceptions;

namespace AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate
{
    /// <summary>
    /// A salon's participation in a platform campaign. Campaigns are opt-in and funded by the salons that join
    /// (decision 2026-09-25), so a campaign applies at a salon only while this is active.
    ///
    /// <para>One row per (campaign, salon): leaving and re-joining toggle it rather than piling up rows. Its own
    /// aggregate — not a child of <see cref="Promotion"/> — because a campaign may have thousands of salons and a
    /// redemption must not load them all.</para>
    /// </summary>
    public sealed class CampaignEnrollment : AggregateRoot<Guid>
    {
        public Guid PromotionId { get; private set; }
        public ProviderId ProviderId { get; private set; } = null!;
        public bool IsActive { get; private set; }
        public DateTime JoinedAt { get; private set; }
        public DateTime? LeftAt { get; private set; }
        public Guid ChangedBy { get; private set; }

        private CampaignEnrollment()
        {
        }

        public static CampaignEnrollment Join(Promotion campaign, ProviderId providerId, Guid joinedBy, DateTime nowUtc)
        {
            EnsureJoinable(campaign, nowUtc);

            return new CampaignEnrollment
            {
                Id = Guid.NewGuid(),
                PromotionId = campaign.Id,
                ProviderId = providerId ?? throw new DomainValidationException(nameof(ProviderId), "سالن مشخص نیست."),
                IsActive = true,
                JoinedAt = nowUtc,
                ChangedBy = joinedBy,
            };
        }

        /// <summary>Joins again after leaving. Already joined: nothing changes.</summary>
        public void Rejoin(Promotion campaign, Guid joinedBy, DateTime nowUtc)
        {
            if (IsActive)
                return;

            EnsureJoinable(campaign, nowUtc);
            IsActive = true;
            JoinedAt = nowUtc;
            LeftAt = null;
            ChangedBy = joinedBy;
        }

        /// <summary>Stops the campaign applying to new bookings here. Bookings already made keep their discount.</summary>
        public void Leave(DateTime nowUtc, Guid? leftBy = null)
        {
            if (!IsActive)
                return;

            IsActive = false;
            LeftAt = nowUtc;
            if (leftBy.HasValue)
                ChangedBy = leftBy.Value;
        }

        private static void EnsureJoinable(Promotion campaign, DateTime nowUtc)
        {
            if (campaign.Owner != PromotionOwner.Platform)
                throw new BusinessRuleViolationException(
                    nameof(CampaignEnrollment), "فقط به کمپین‌های پلتفرم می‌توان پیوست.", "NOT_A_CAMPAIGN");

            if (!campaign.CanBeJoinedAt(nowUtc))
                throw new BusinessRuleViolationException(
                    nameof(CampaignEnrollment), "این کمپین به پایان رسیده است.", "CAMPAIGN_OVER");
        }
    }
}
