using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Repositories
{
    public sealed class PromotionRepository : IPromotionRepository
    {
        private readonly ServiceCatalogDbContext _context;

        public PromotionRepository(ServiceCatalogDbContext context) => _context = context;

        public Task<Promotion?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.Promotions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        public async Task<IReadOnlyList<Promotion>> GetManyAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            ids.Count == 0
                ? Array.Empty<Promotion>()
                : await _context.Promotions.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Promotion>> ListForProviderAsync(
            ProviderId providerId, CancellationToken cancellationToken = default) =>
            await _context.Promotions.AsNoTracking()
                .Where(p => p.Owner == PromotionOwner.Provider && p.ProviderId == providerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Promotion>> ListJoinableCampaignsAsync(
            DateTime nowUtc, CancellationToken cancellationToken = default) =>
            await _context.Promotions.AsNoTracking()
                .Where(p => p.Owner == PromotionOwner.Platform
                            && p.Status != PromotionStatus.Ended
                            && (p.EndsAt == null || p.EndsAt > nowUtc))
                .OrderBy(p => p.StartsAt)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<PromotionWithEnrollment>> GetPricingCandidatesAsync(
            ProviderId providerId, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            var promotions = await _context.Promotions.AsNoTracking()
                .Where(p => p.Status != PromotionStatus.Ended
                            && (p.EndsAt == null || p.EndsAt > nowUtc)
                            && ((p.Owner == PromotionOwner.Provider && p.ProviderId == providerId)
                                || p.Owner == PromotionOwner.Platform))
                .ToListAsync(cancellationToken);

            var campaignIds = promotions.Where(p => p.Owner == PromotionOwner.Platform).Select(p => p.Id).ToList();
            var joined = campaignIds.Count == 0
                ? new HashSet<Guid>()
                : (await _context.CampaignEnrollments.AsNoTracking()
                    .Where(e => e.ProviderId == providerId && e.IsActive && campaignIds.Contains(e.PromotionId))
                    .Select(e => e.PromotionId)
                    .ToListAsync(cancellationToken)).ToHashSet();

            return promotions
                .Select(p => new PromotionWithEnrollment(p, p.Owner == PromotionOwner.Provider || joined.Contains(p.Id)))
                .ToList();
        }

        public Task<bool> CodeInUseAsync(
            PromotionOwner owner, ProviderId? providerId, string code, Guid? excludingId,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Promotions.AsNoTracking()
                .Where(p => p.Owner == owner && p.Code == code && p.Status != PromotionStatus.Ended);
            if (owner == PromotionOwner.Provider)
                query = query.Where(p => p.ProviderId == providerId);
            if (excludingId.HasValue)
                query = query.Where(p => p.Id != excludingId.Value);
            return query.AnyAsync(cancellationToken);
        }

        public async Task<PromotionPage> SearchAsync(PromotionSearch search, CancellationToken cancellationToken = default)
        {
            var query = _context.Promotions.AsNoTracking().AsQueryable();
            if (search.Owner.HasValue)
                query = query.Where(p => p.Owner == search.Owner.Value);
            if (search.ProviderId is not null)
                query = query.Where(p => p.ProviderId == search.ProviderId);
            if (search.Status.HasValue)
                query = query.Where(p => p.Status == search.Status.Value);
            if (!string.IsNullOrWhiteSpace(search.Text))
            {
                var text = search.Text.Trim();
                var code = text.ToUpperInvariant();
                query = query.Where(p => EF.Functions.ILike(p.Title, "%" + text + "%") || p.Code == code);
            }

            var total = await query.CountAsync(cancellationToken);
            var page = Math.Max(1, search.Page);
            var size = Math.Clamp(search.PageSize, 1, 100);
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync(cancellationToken);
            return new PromotionPage(items, total);
        }

        public Task<bool> HasPriorBookingAsync(Guid customerId, ProviderId providerId, CancellationToken cancellationToken = default)
        {
            var customer = Core.Domain.ValueObjects.UserId.From(customerId);
            return _context.Bookings.AsNoTracking().AnyAsync(
                b => b.CustomerId == customer
                     && b.ProviderId == providerId
                     && b.Status != BookingStatus.Cancelled
                     && b.Status != BookingStatus.Rescheduled,
                cancellationToken);
        }

        public async Task<IReadOnlyDictionary<Guid, string>> ProviderNamesAsync(
            IReadOnlyCollection<Guid> providerIds, CancellationToken cancellationToken = default)
        {
            if (providerIds.Count == 0)
                return new Dictionary<Guid, string>();

            var ids = providerIds.Select(ProviderId.From).ToList();
            var rows = await _context.Providers.AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .Select(p => new { p.Id, p.Profile.BusinessName })
                .ToListAsync(cancellationToken);
            return rows.ToDictionary(r => r.Id.Value, r => r.BusinessName);
        }

        public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default) =>
            await _context.Promotions.AddAsync(promotion, cancellationToken);
    }

    public sealed class CampaignEnrollmentRepository : ICampaignEnrollmentRepository
    {
        private readonly ServiceCatalogDbContext _context;

        public CampaignEnrollmentRepository(ServiceCatalogDbContext context) => _context = context;

        public Task<CampaignEnrollment?> GetAsync(Guid promotionId, ProviderId providerId, CancellationToken cancellationToken = default) =>
            _context.CampaignEnrollments.FirstOrDefaultAsync(
                e => e.PromotionId == promotionId && e.ProviderId == providerId, cancellationToken);

        public async Task<IReadOnlySet<Guid>> ActiveCampaignIdsAsync(ProviderId providerId, CancellationToken cancellationToken = default) =>
            (await _context.CampaignEnrollments.AsNoTracking()
                .Where(e => e.ProviderId == providerId && e.IsActive)
                .Select(e => e.PromotionId)
                .ToListAsync(cancellationToken)).ToHashSet();

        public async Task<IReadOnlyList<CampaignEnrollment>> ListActiveAsync(Guid promotionId, CancellationToken cancellationToken = default) =>
            await _context.CampaignEnrollments.AsNoTracking()
                .Where(e => e.PromotionId == promotionId && e.IsActive)
                .OrderByDescending(e => e.JoinedAt)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyDictionary<Guid, int>> ActiveCountsAsync(
            IReadOnlyCollection<Guid> promotionIds, CancellationToken cancellationToken = default)
        {
            if (promotionIds.Count == 0)
                return new Dictionary<Guid, int>();

            return await _context.CampaignEnrollments.AsNoTracking()
                .Where(e => e.IsActive && promotionIds.Contains(e.PromotionId))
                .GroupBy(e => e.PromotionId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        }

        public async Task AddAsync(CampaignEnrollment enrollment, CancellationToken cancellationToken = default) =>
            await _context.CampaignEnrollments.AddAsync(enrollment, cancellationToken);
    }

    public sealed class PromotionRedemptionRepository : IPromotionRedemptionRepository
    {
        private readonly ServiceCatalogDbContext _context;

        public PromotionRedemptionRepository(ServiceCatalogDbContext context) => _context = context;

        public Task<PromotionRedemption?> GetAppliedForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
            _context.PromotionRedemptions.FirstOrDefaultAsync(
                r => r.BookingId == bookingId && r.Status == PromotionRedemptionStatus.Applied, cancellationToken);

        public async Task<IReadOnlyDictionary<Guid, int>> AppliedCountsForCustomerAsync(
            Guid customerId, IReadOnlyCollection<Guid> promotionIds, CancellationToken cancellationToken = default)
        {
            if (promotionIds.Count == 0)
                return new Dictionary<Guid, int>();

            return await _context.PromotionRedemptions.AsNoTracking()
                .Where(r => r.CustomerId == customerId
                            && r.Status == PromotionRedemptionStatus.Applied
                            && promotionIds.Contains(r.PromotionId))
                .GroupBy(r => r.PromotionId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        }

        public async Task<IReadOnlyDictionary<Guid, PromotionUsage>> UsageAsync(
            IReadOnlyCollection<Guid> promotionIds, CancellationToken cancellationToken = default)
        {
            if (promotionIds.Count == 0)
                return new Dictionary<Guid, PromotionUsage>();

            return await _context.PromotionRedemptions.AsNoTracking()
                .Where(r => r.Status == PromotionRedemptionStatus.Applied && promotionIds.Contains(r.PromotionId))
                .GroupBy(r => r.PromotionId)
                .Select(g => new { g.Key, Uses = g.Count(), Total = g.Sum(r => r.Amount) })
                .ToDictionaryAsync(x => x.Key, x => new PromotionUsage(x.Uses, x.Total), cancellationToken);
        }

        public async Task AddAsync(PromotionRedemption redemption, CancellationToken cancellationToken = default) =>
            await _context.PromotionRedemptions.AddAsync(redemption, cancellationToken);
    }
}
