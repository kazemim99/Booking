using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Application.Abstractions.Queries;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Specifications;
using Microsoft.Extensions.Logging;


namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServicesByProvider
{
    public sealed class GetServicesByProviderQueryHandler : IQueryHandler<GetServicesByProviderQuery, PagedResult<ServiceSummaryItem>>
    {
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetServicesByProviderQueryHandler> _logger;

        public GetServicesByProviderQueryHandler(
            IServiceReadRepository serviceRepository,
            ILogger<GetServicesByProviderQueryHandler> logger)
        {
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<PagedResult<ServiceSummaryItem>> Handle(
            GetServicesByProviderQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Processing services search for provider: {ProviderId} with filters: {@Filters}",
                request.ProviderId, new
                {
                    request.Status,
                    request.Category,
                });

            try
            {
                var providerId = ProviderId.Create(request.ProviderId);

                // Create business specification (no pagination logic!)
                var specification = new GetServicesByProviderSpecification(
                    providerId: providerId,
                    status: request.Status,
                    category: request.Category,
                    includeOptions: false,
                    includePriceTiers: false,
                    includeProvider: false);


                // Use generic pagination extension - One line handles everything!
                var result = await _serviceRepository.GetPaginatedAsync(
                    specification,
                    request.Pagination,
                    service => new ServiceSummaryItem(
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
                        service.ImageUrl),
                    cancellationToken);

                _logger.LogInformation("Services search for provider {ProviderId} completed. Found {TotalCount} services, returning page {PageNumber} of {PageSize}",
                    request.ProviderId, result.TotalCount, request.Pagination.PageNumber, request.Pagination.PageSize);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during services search for provider {ProviderId} with query: {@Query}",
                    request.ProviderId, request);
                throw;
            }
        }
    }
}
