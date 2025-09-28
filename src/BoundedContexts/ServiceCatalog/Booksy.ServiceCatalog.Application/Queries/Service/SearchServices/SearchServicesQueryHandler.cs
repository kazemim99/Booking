using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Application.Abstractions.Queries;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Specifications;
using Microsoft.Extensions.Logging;



namespace Booksy.ServiceCatalog.Application.Queries.Service.SearchServices
{
    public sealed class SearchServicesQueryHandler : IQueryHandler<SearchServicesQuery, PagedResult<ServiceSearchItem>>
    {
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<SearchServicesQueryHandler> _logger;

        public SearchServicesQueryHandler(
            IServiceReadRepository serviceRepository,
            ILogger<SearchServicesQueryHandler> logger)
        {
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<PagedResult<ServiceSearchItem>> Handle(
            SearchServicesQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Processing services search with filters: {@Filters}", new
            {
                request.SearchTerm,
                request.Category,
                request.Type,
                request.MinPrice,
                request.MaxPrice,
                request.MaxDurationMinutes,
                request.AvailableAsMobile,
                request.City,
                request.State
            });

            try
            {
                // Create business specification (no pagination logic!)
                var specification = new SearchServicesSpecification(
                    searchTerm: request.SearchTerm,
                    category: request.Category,
                    type: request.Type,
                    minPrice: request.MinPrice,
                    maxPrice: request.MaxPrice,
                    maxDurationMinutes: request.MaxDurationMinutes,
                    availableAsMobile: request.AvailableAsMobile,
                    city: request.City,
                    state: request.State,
                    status: null, // Search only active services by default
                    includeProvider: true, // Need provider info for search results
                    includeOptions: false,
                    includePriceTiers: false);


                // Use generic pagination extension - One line handles everything!
                var result = await _serviceRepository.GetPaginatedAsync(
                    specification,
                    request.Pagination,
                    service => new ServiceSearchItem(
                         service.Id.Value,
                         service.ProviderId.Value,
                         service.Name,
                         service.Description,
                    service.Category.Name,
                      service.Type.ToString(),
                   service.BasePrice.Amount,
                    service.BasePrice.Currency,
                       service.Duration.Value,
                       service.Status.ToString(),
                    service.RequiresDeposit,
                        service.AvailableAsMobile,
                      service.ImageUrl,
                      new ProviderInfo(
                             service.ProviderId.Value,
                            service.Provider.Profile.BusinessName,
                            service.Provider.Address.City,
                            service.Provider.Address.State,
                            service.Provider.AllowOnlineBooking,
                            service.Provider.OffersMobileServices)),
                    cancellationToken);

                _logger.LogInformation("Services search completed. Found {TotalCount} services, returning page {PageNumber} of {PageSize}",
                    result.TotalCount, request.Pagination.PageNumber, request.Pagination.PageSize);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during services search with query: {@Query}", request);
                throw;
            }
        }
    }
}