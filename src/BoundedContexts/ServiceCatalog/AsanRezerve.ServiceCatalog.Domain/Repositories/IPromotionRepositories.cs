using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;

namespace AsanRezerve.ServiceCatalog.Domain.Repositories
{
    /// <summary>Promotions: salons' own and platform campaigns (openspec/changes/add-discounts-and-campaigns).</summary>
    public interface IPromotionRepository
    {
        Task<Promotion?> GetAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Tracked promotions by id, for pricing a booking and recording its redemption.</summary>
        Task<IReadOnlyList<Promotion>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Promotion>> ListForProviderAsync(ProviderId providerId, CancellationToken cancellationToken = default);

        /// <summary>Platform campaigns a salon may still join: not ended and not past their end.</summary>
        Task<IReadOnlyList<Promotion>> ListJoinableCampaignsAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

        /// <summary>
        /// Everything that could price a visit at this salon: its own promotions and every platform campaign, each
        /// with whether the salon has joined it. Ended promotions and those past their end are left out.
        /// </summary>
        Task<IReadOnlyList<PromotionWithEnrollment>> GetPricingCandidatesAsync(
            ProviderId providerId, DateTime nowUtc, CancellationToken cancellationToken = default);

        /// <summary>Whether another promotion in the same owner scope already uses this code.</summary>
        Task<bool> CodeInUseAsync(
            PromotionOwner owner, ProviderId? providerId, string code, Guid? excludingId,
            CancellationToken cancellationToken = default);

        Task<PromotionPage> SearchAsync(PromotionSearch search, CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether the customer has an earlier booking at this salon that still counts (not cancelled, not replaced
        /// by a reschedule). "New customer" promotions apply only when this is false.
        /// </summary>
        Task<bool> HasPriorBookingAsync(Guid customerId, ProviderId providerId, CancellationToken cancellationToken = default);

        /// <summary>Salons' business names by id, for admin lists.</summary>
        Task<IReadOnlyDictionary<Guid, string>> ProviderNamesAsync(
            IReadOnlyCollection<Guid> providerIds, CancellationToken cancellationToken = default);

        Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default);
    }

    public sealed record PromotionWithEnrollment(Promotion Promotion, bool IsEnrolled);

    public sealed record PromotionSearch(
        PromotionOwner? Owner,
        ProviderId? ProviderId,
        PromotionStatus? Status,
        string? Text,
        int Page,
        int PageSize);

    public sealed record PromotionPage(IReadOnlyList<Promotion> Items, int TotalCount);

    public interface ICampaignEnrollmentRepository
    {
        Task<CampaignEnrollment?> GetAsync(Guid promotionId, ProviderId providerId, CancellationToken cancellationToken = default);

        /// <summary>The campaigns this salon is currently in.</summary>
        Task<IReadOnlySet<Guid>> ActiveCampaignIdsAsync(ProviderId providerId, CancellationToken cancellationToken = default);

        /// <summary>Salons currently in this campaign, most recent first.</summary>
        Task<IReadOnlyList<CampaignEnrollment>> ListActiveAsync(Guid promotionId, CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, int>> ActiveCountsAsync(IReadOnlyCollection<Guid> promotionIds, CancellationToken cancellationToken = default);

        Task AddAsync(CampaignEnrollment enrollment, CancellationToken cancellationToken = default);
    }

    public interface IPromotionRedemptionRepository
    {
        /// <summary>The discount a booking currently holds, if any (tracked).</summary>
        Task<PromotionRedemption?> GetAppliedForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

        /// <summary>Per promotion: how many bookings this customer currently holds with it.</summary>
        Task<IReadOnlyDictionary<Guid, int>> AppliedCountsForCustomerAsync(
            Guid customerId, IReadOnlyCollection<Guid> promotionIds, CancellationToken cancellationToken = default);

        /// <summary>Per promotion: applied uses and the total discount they gave.</summary>
        Task<IReadOnlyDictionary<Guid, PromotionUsage>> UsageAsync(
            IReadOnlyCollection<Guid> promotionIds, CancellationToken cancellationToken = default);

        Task AddAsync(PromotionRedemption redemption, CancellationToken cancellationToken = default);
    }

    public sealed record PromotionUsage(int Uses, decimal TotalDiscount);
}
