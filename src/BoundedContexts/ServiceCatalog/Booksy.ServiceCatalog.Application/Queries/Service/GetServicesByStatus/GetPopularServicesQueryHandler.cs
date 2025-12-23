using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Specifications;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetPopularServices
{
    public sealed class GetPopularServicesQueryHandler : IQueryHandler<GetPopularServicesQuery, IReadOnlyList<ServiceSummaryItem>>
    {
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetPopularServicesQueryHandler> _logger;

        public GetPopularServicesQueryHandler(
            IServiceReadRepository serviceRepository,
            ILogger<GetPopularServicesQueryHandler> logger)
        {
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ServiceSummaryItem>> Handle(
            GetPopularServicesQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting popular services, Category: {Category}, Limit: {Limit}",
                request.CategoryFilter, request.Limit);

            try
            {
                var specification = new PopularServicesSpecification(request.CategoryFilter);

                var services = await _serviceRepository.GetAsync(specification, cancellationToken);

                var result = services
                    .Take(request.Limit)
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

                _logger.LogInformation("Retrieved {Count} popular services", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular services");
                throw;
            }
        }
    }
}
