using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    public sealed class GetProviderByIdQueryHandler : IQueryHandler<GetProviderByIdQuery, ProviderDetailsViewModel?>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetProviderByIdQueryHandler> _logger;

        public GetProviderByIdQueryHandler(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            ILogger<GetProviderByIdQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<ProviderDetailsViewModel?> Handle(
            GetProviderByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting provider details for ID: {ProviderId}", request.ProviderId);

            var providerId = ProviderId.Create(request.ProviderId);
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null)
            {
                _logger.LogWarning("Provider not found: {ProviderId}", request.ProviderId);
                return null;
            }

            var viewModel = new ProviderDetailsViewModel
            {

                Id = provider.Id.Value,
                OwnerId = provider.OwnerId.Value,
                BusinessName = provider.Profile.BusinessName,
                Description = provider.Profile.BusinessDescription,
                LogoUrl = provider.Profile.LogoUrl,
                Status = provider.Status,
                Type = provider.Type,
                
            };

            if (request.IncludeStaff)
            {
                viewModel.Staff = provider.Staff.Select(staff => new StaffViewModel
                {
                    Id = staff.Id,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    FullName = staff.FullName,
                    Email = staff.Email.Value,
                    Phone = staff.Phone?.Value,
                    Role = staff.Role,
                    IsActive = staff.IsActive,
                    HiredAt = staff.HiredAt
                }).ToList();
            }

            if (request.IncludeServices)
            {
                var services = await _serviceRepository.GetByProviderIdAndStatusAsync(
                    providerId,
                    Domain.Enums.ServiceStatus.Active,
                    cancellationToken);

                viewModel.ActiveServicesCount = services.Count;
                viewModel.Services = services.Select(service => new ServiceSummaryViewModel
                {
                    Id = service.Id.Value,
                    Name = service.Name,
                    Description = service.Description,
                    Category = service.Category.Name,
                    BasePrice = service.BasePrice.Amount,
                    Currency = service.BasePrice.Currency,
                    Duration = service.Duration.Value,
                    Status = service.Status
                }).ToList();
            }

            return viewModel;
        }
    }
}