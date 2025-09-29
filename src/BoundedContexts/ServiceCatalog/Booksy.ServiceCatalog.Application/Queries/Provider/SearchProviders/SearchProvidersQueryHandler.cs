
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
                request.Type,
                request.City,
                request.State,
                request.Country,
                request.AllowsOnlineBooking,
                request.OffersMobileServices,
                request.VerifiedOnly,
                request.MinRating,
                request.IncludeInactive,
            });

            try
            {
                // Create business specification (no pagination logic!)
                var specification = new SearchProvidersSpecification(
                    searchTerm: request.SearchTerm,
                    type: request.Type,
                    city: request.City,
                    state: request.State,
                    country: request.Country,
                    allowsOnlineBooking: request.AllowsOnlineBooking,
                    offersMobileServices: request.OffersMobileServices,
                    verifiedOnly: request.VerifiedOnly,
                    minRating: request.MinRating,
                    includeInactive: request.IncludeInactive);


                // Use generic pagination extension - One line handles everything!
                var result = await _providerRepository.GetPaginatedAsync(
                    specification,
                    request.Pagination,
                    provider => new ProviderSearchItem(
                         provider.Id.Value,
                         provider.Profile.BusinessName,
                        provider.Profile.BusinessDescription,
                         provider.Size,
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
                        provider.LastActiveAt),
                    cancellationToken);

                _logger.LogInformation("Provider search completed. Found {TotalCount} providers, returning page {PageNumber} of {PageSize}",
                    result.TotalCount, request.Pagination.PageNumber, request.Pagination.PageSize);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during provider search with query: {@Query}", request);
                throw;
            }
        }

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

