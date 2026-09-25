//===========================================
// Provider Query Definitions
//===========================================

//===========================================
// Queries/Provider/SearchProviders/SearchProvidersQuery.cs
//===========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.CQRS;
using AsanRezerve.Core.Application.DTOs;
using AsanRezerve.ServiceCatalog.Application.Caching;
using AsanRezerve.ServiceCatalog.Application.DTOs.Provider;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.SearchProviders
{
    public sealed record SearchProvidersQuery(
        string? SearchTerm = null,
        ServiceCategory? Category = null,
        string? City = null,
        string? State = null,
        string? Country = null,
        bool? AllowsOnlineBooking = null,
        bool? OffersMobileServices = null,
        bool? VerifiedOnly = null,
        decimal? MinRating = null,
        string? ServiceCategory = null,
        DateTime? AvailableOn = null,
        string? PriceRange = null,
        string SortBy = "rating",
        bool SortDescending = true,
        double? UserLatitude = null,
        double? UserLongitude = null,
        bool IncludeInactive = false) : PaginatedQueryBase<ProviderSearchItem>()
    {
        /// <summary>
        /// Cached for a minute, except around the user's position: every user stands somewhere else, so those
        /// entries would almost never be hit again. Evicted by any provider change.
        /// </summary>
        public override bool IsCacheable => UserLatitude is null && UserLongitude is null;

        public override int? CacheExpirationSeconds => 60;

        public override IReadOnlyCollection<string>? CacheTags => [ReadModelCacheTags.ProviderDirectory];
    }
}

