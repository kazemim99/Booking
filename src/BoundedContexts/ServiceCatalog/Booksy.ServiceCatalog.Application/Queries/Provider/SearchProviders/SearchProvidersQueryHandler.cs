
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Specifications.Provider;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders
{
    /// <summary>
    /// Handler following User Management pattern with specifications and generic pagination
    /// </summary>
    public sealed class SearchProvidersQueryHandler : IQueryHandler<SearchProvidersQuery, PagedResult<ProviderSearchItem>>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly ILogger<SearchProvidersQueryHandler> _logger;

        public SearchProvidersQueryHandler(
            IProviderReadRepository providerRepository,
            ILogger<SearchProvidersQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _logger = logger;
        }

        public async Task<PagedResult<ProviderSearchItem>> Handle(
            SearchProvidersQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Processing provider search with filters: {@Filters}", new
            {
                request.SearchTerm,
                request.Category,
                request.City,
                request.State,
                request.Country,
                request.AllowsOnlineBooking,
                request.OffersMobileServices,
                request.VerifiedOnly,
                request.MinRating,
                request.ServiceCategory,
                request.PriceRange,
                request.SortBy,
                request.SortDescending,
                request.IncludeInactive,
            });

            try
            {
                // Create business specification with new filters
                var specification = new SearchProvidersSpecification(
                    searchTerm: request.SearchTerm,
                    // `category` was omitted here, so SearchProvidersQuery.Category was a dead filter:
                    // it was accepted and logged, then every provider came back regardless of category.
                    category: request.Category,
                    city: request.City,
                    state: request.State,
                    country: request.Country,
                    allowsOnlineBooking: request.AllowsOnlineBooking,
                    offersMobileServices: request.OffersMobileServices,
                    verifiedOnly: request.VerifiedOnly,
                    minRating: request.MinRating,
                    serviceCategory: request.ServiceCategory,
                    priceRange: request.PriceRange,
                    includeInactive: request.IncludeInactive,
                    excludeStaffIndividuals: true); // Filter out staff individuals from search results

                // Apply dynamic sorting based on request
                ApplySorting(specification, request.SortBy, request.SortDescending, request.UserLatitude, request.UserLongitude);

                // Use generic pagination extension
                var result = await _providerRepository.GetPaginatedAsync(
                    specification,
                    request.Pagination,
                    provider => new ProviderSearchItem(
                         provider.Id.Value,
                         provider.Profile.BusinessName,
                        provider.Profile.BusinessDescription,
                        provider.Profile.ProfileImageUrl,
                         provider.PrimaryCategory,
                        provider.Status,
                            provider.Address.City,
                        provider.Address.State,
                        provider.Address.Country,
                        provider.Profile.LogoUrl,
                        provider.AllowOnlineBooking,
                        provider.OffersMobileServices,
                        provider.AverageRating,
                                provider.Services.Count,
                         DateTime.UtcNow.Year - provider.RegisteredAt.Year,
                        provider.Status == ProviderStatus.Verified,
                       //OperatingHours: GetFormattedOperatingHours(provider.BusinessHours),
                       provider.RegisteredAt,
                        provider.LastActiveAt,
                        // Hierarchy information
                        provider.HierarchyType,
                        provider.IsIndependent,
                        provider.ParentProviderId != null ? provider.ParentProviderId.Value : (Guid?)null,
                        null, // ParentProviderName - would require join, can be fetched separately
                        0), // StaffProviderCount - would require count query, can be fetched separately
                    cancellationToken);

                _logger.LogInformation("Provider search completed. Found {TotalCount} providers, returning page {PageNumber} of {PageSize}. Sort: {SortBy} {Direction}",
                    result.TotalCount, request.Pagination.PageNumber, request.Pagination.PageSize, request.SortBy, request.SortDescending ? "DESC" : "ASC");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during provider search with query: {@Query}", request);
                throw;
            }
        }

        /// <summary>
        /// Applies dynamic sorting to the specification based on the sort parameter
        /// </summary>
        private static void ApplySorting(
            SearchProvidersSpecification specification,
            string sortBy,
            bool sortDescending,
            double? userLatitude,
            double? userLongitude)
        {
            switch (sortBy.ToLowerInvariant())
            {
                case "rating":
                    if (sortDescending)
                        specification.AddOrderByDescending(p => p.AverageRating);
                    else
                        specification.AddOrderBy(p => p.AverageRating);
                    specification.AddThenBy(p => p.Profile.BusinessName);
                    break;

                case "distance":
                    // "Sort by distance" used to sort by RATING — both branches below were identical, so asking
                    // for nearest-first returned the highest-rated first and the caller had no way to tell. In
                    // Parsabad that put the farthest provider at the top of "near me".
                    //
                    // PostGIS is not required to order by proximity. Ordering only needs a value that increases
                    // with real distance, so this uses squared planar distance with the longitude axis scaled by
                    // cos(latitude) to correct for meridian convergence. `lonScale` is computed here in C#, so
                    // the expression is plain arithmetic over two columns and translates to SQL. Over a city it
                    // ranks identically to Haversine, and skipping the square root keeps it exact in integers
                    // rather than introducing rounding.
                    //
                    // True distances in km are still CalculateDistance's job; this is strictly the ORDER BY.
                    if (userLatitude.HasValue && userLongitude.HasValue)
                    {
                        var lat0 = userLatitude.Value;
                        var lon0 = userLongitude.Value;
                        var lonScale = Math.Cos(lat0 * Math.PI / 180.0);

                        // Providers with no coordinates cannot be ranked by proximity; they sort last rather
                        // than being dropped from results or landing at position zero.
                        System.Linq.Expressions.Expression<Func<Domain.Aggregates.Provider, object>> byProximity =
                            p => p.Address.Latitude == null || p.Address.Longitude == null
                                ? double.MaxValue
                                : ((p.Address.Latitude.Value - lat0) * (p.Address.Latitude.Value - lat0))
                                  + ((p.Address.Longitude.Value - lon0) * lonScale)
                                    * ((p.Address.Longitude.Value - lon0) * lonScale);

                        // Ascending is nearest-first, which is what "near me" means.
                        if (sortDescending)
                            specification.AddOrderByDescending(byProximity);
                        else
                            specification.AddOrderBy(byProximity);

                        // Deterministic tie-break so equal-distance providers keep a stable page order.
                        specification.AddThenBy(p => p.Profile.BusinessName);
                    }
                    else
                    {
                        // Without a reference point there is no distance to sort by; fall back to rating, as
                        // before, but only in the case where that is genuinely the best available ordering.
                        specification.AddOrderByDescending(p => p.AverageRating);
                        specification.AddThenBy(p => p.Profile.BusinessName);
                    }
                    break;

                case "name":
                    if (sortDescending)
                        specification.AddOrderByDescending(p => p.Profile.BusinessName);
                    else
                        specification.AddOrderBy(p => p.Profile.BusinessName);
                    break;

                default:
                    if (sortDescending)
                        specification.AddOrderByDescending(p => p.CreatedAt);
                    else
                        specification.AddOrderBy(p => p.CreatedAt);

                    specification.AddThenBy(p => p.Status);

                    break;
            }
        }

        /// <summary>
        /// Calculate distance between two points using Haversine formula
        /// Returns distance in kilometers
        /// </summary>
        private static double CalculateDistance(
            double lat1, double lon1,
            double lat2, double lon2)
        {
            const double R = 6371; // Earth radius in kilometers

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // Distance in kilometers
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

        private static string? GetFormattedOperatingHours(Dictionary<DayOfWeek, BusinessHoursDto?> businessHours)
        {
            if (!businessHours.Any(bh => bh.Value != null))
                return null;

            var openDays = businessHours
                .Where(bh => bh.Value != null)
                .Select(bh => $"{bh.Key}: {bh.Value.OpenTime:HH:mm}-{bh.Value.CloseTime:HH:mm}")
                .ToList();

            return string.Join(", ", openDays);
        }
    }
}

