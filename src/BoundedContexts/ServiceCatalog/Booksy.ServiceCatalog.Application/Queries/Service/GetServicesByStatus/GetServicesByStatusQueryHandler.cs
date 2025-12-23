using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Specifications;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServicesByStatus
{
    public sealed class GetServicesByStatusQueryHandler : IQueryHandler<GetServicesByStatusQuery, IReadOnlyList<ServiceSummaryItem>>
    {
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetServicesByStatusQueryHandler> _logger;

        public GetServicesByStatusQueryHandler(
            IServiceReadRepository serviceRepository,
            ILogger<GetServicesByStatusQueryHandler> logger)
        {
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ServiceSummaryItem>> Handle(
            GetServicesByStatusQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting services by status: {Status}, MaxResults: {MaxResults}",
                request.Status, request.MaxResults);

            try
            {
                var specification = new ServicesByStatusSpecification(request.Status);

                // For simple non-paginated queries, we can use a simpler approach
                var services = await _serviceRepository.GetAsync(specification, cancellationToken);

                var result = services
                    .Take(request.MaxResults)
                    .Select(service => new ServiceSummaryItem(
                         service.Id.Value,
                       service.ProviderId.Value,
                     service.Name,
                        service.Description,
                       service.Category.ToEnglishName(),
                       service.Type.ToString(),
                        service.BasePrice.Amount,
                        service.BasePrice.Currency,
                        service.Duration.Value,
                        service.Status.ToString(),
                            service.RequiresDeposit,
                         service.AvailableAsMobile,
                        service.ImageUrl))
                    .ToList();

                _logger.LogInformation("Retrieved {Count} services with status {Status}",
                    result.Count, request.Status);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by status {Status}", request.Status);
                throw;
            }
        }
    }
}
