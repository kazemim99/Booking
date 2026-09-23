// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetCustomerFavoriteProviders/SalonSummaryLookup.cs
// ========================================
using Booksy.ServiceCatalog.Application.Abstractions;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerFavoriteProviders
{
    /// <summary>What a customer's salon list shows about one salon: enough to draw its card.</summary>
    internal sealed record SalonSummary(
        string Name,
        string? LogoUrl,
        string? City,
        decimal AverageRating,
        int TotalReviews);

    /// <summary>
    /// The salons behind a customer's favourites and recent visits, read from ServiceCatalog in ONE
    /// query. The customer aggregate stores only salon ids; sent alone, the app had no name to draw and
    /// failed the whole Home section (customer-app-ux-review-fixes P1).
    ///
    /// Only Active salons come back: a salon that was deleted, archived or suspended since the customer
    /// saved or opened it is left out of the list rather than sent as a row the app cannot open.
    /// </summary>
    internal static class SalonSummaryLookup
    {
        public static async Task<IReadOnlyDictionary<Guid, SalonSummary>> FindActiveAsync(
            IProviderReadRepository providers,
            IUrlService urls,
            IEnumerable<Guid> providerIds,
            CancellationToken cancellationToken)
        {
            var ids = providerIds.Distinct().Select(ProviderId.From).ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, SalonSummary>();

            var salons = await providers.GetAsync(new ActiveSalonsByIdsSpecification(ids), cancellationToken);

            return salons.ToDictionary(
                p => p.Id.Value,
                p => new SalonSummary(
                    p.Profile.BusinessName,
                    urls.AbsoluteOrNull(p.Profile.DisplayImageUrl),
                    p.Address?.City,
                    p.AverageRating,
                    p.PublishedReviewCount));
        }

        private sealed class ActiveSalonsByIdsSpecification : BaseSpecification<Provider>
        {
            public ActiveSalonsByIdsSpecification(List<ProviderId> ids)
                : base(p => ids.Contains(p.Id) && p.Status == ProviderStatus.Active)
            {
            }
        }
    }
}
