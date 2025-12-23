
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
                    city: request.City,
                    state: request.State,
                    country: request.Country,
                    allowsOnlineBooking: request.AllowsOnlineBooking,
                    offersMobileServices: request.OffersMobileServices,
                    verifiedOnly: request.VerifiedOnly,
                    minRating: request.MinRating,
                    serviceCategory:  request.ServiceCategory,
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
                    // Note: Distance sorting requires geospatial calculation
                    // For now, fallback to rating if coordinates not provided
                    // TODO: Implement PostGIS distance calculation in Phase 4
                    if (userLatitude.HasValue && userLongitude.HasValue)
                    {
                        // Distance sorting will be implemented with PostGIS in Phase 4
                        // For now, we'll use rating as fallback
                        specification.AddOrderByDescending(p => p.AverageRating);
                    }
                    else
                    {
                        specification.AddOrderByDescending(p => p.AverageRating);
                    }
                    break;

                case "name":
                    if (sortDescending)
                        specification.AddOrderByDescending(p => p.Profile.BusinessName);
                    else
                        specification.AddOrderBy(p => p.Profile.BusinessName);
                    break;

                default:
                    // Default: sort by rating descending
                    specification.AddOrderByDescending(p => p.AverageRating);
                    specification.AddThenBy(p => p.Profile.BusinessName);
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

