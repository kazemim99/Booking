// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetServiceById/GetServiceByIdQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetServiceById
{
    public sealed class GetServiceByIdQueryHandler : IQueryHandler<GetServiceByIdQuery, ServiceDetailsViewModel?>
    {
        private readonly IServiceReadRepository _serviceRepository;
        private readonly IProviderReadRepository _providerRepository;
        private readonly ILogger<GetServiceByIdQueryHandler> _logger;

        public GetServiceByIdQueryHandler(
            IServiceReadRepository serviceRepository,
            IProviderReadRepository providerRepository,
            ILogger<GetServiceByIdQueryHandler> logger)
        {
            _serviceRepository = serviceRepository;
            _providerRepository = providerRepository;
            _logger = logger;
        }

        public async Task<ServiceDetailsViewModel?> Handle(
            GetServiceByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting service details for ID: {ServiceId}", request.ServiceId);

            var serviceId = ServiceId.From(request.ServiceId);
            var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);

            if (service == null)
            {
                _logger.LogWarning("Service not found: {ServiceId}", request.ServiceId);
                return null;
            }

            var viewModel = new ServiceDetailsViewModel
            {
                Id = service.Id.Value,
                ProviderId = service.ProviderId.Value,
                Name = service.Name,
                Description = service.Description,
                Category = service.Category.ToEnglishName(),
                Type = service.Type,
                BasePrice = service.BasePrice.Amount,
                Currency = service.BasePrice.Currency,
                Duration = service.Duration.Value,
                PreparationTime = service.PreparationTime?.Value,
                BufferTime = service.BufferTime?.Value,
                TotalDuration = service.GetTotalDuration().Value,
                Status = service.Status,
                RequiresDeposit = service.RequiresDeposit,
                DepositPercentage = service.DepositPercentage,
                DepositAmount = service.RequiresDeposit ? service.CalculateDepositAmount().Amount : 0,
                AllowOnlineBooking = service.AllowOnlineBooking,
                AvailableAtLocation = service.AvailableAtLocation,
                AvailableAsMobile = service.AvailableAsMobile,
                MaxAdvanceBookingDays = service.MaxAdvanceBookingDays ?? 90,
                MinAdvanceBookingHours = service.MinAdvanceBookingHours ?? 1,
                MaxConcurrentBookings = service.MaxConcurrentBookings ?? 1,
                ImageUrl = service.ImageUrl,
              
                CanBeBooked = service.CanBeBooked(),
                CreatedAt = service.CreatedAt,
                ActivatedAt = service.ActivatedAt
            };

            if (request.IncludeOptions && service.Options.Any())
            {
                viewModel.Options = service.Options.Select(option => new ServiceOptionViewModel
                {
                    Id = option.Id,
                    Name = option.Name,
                    Description = option.Description,
                    AdditionalPrice = option.AdditionalPrice.Amount,
                    Currency = option.AdditionalPrice.Currency,
                    AdditionalDuration = option.AdditionalDuration?.Value,
                    IsRequired = option.IsRequired,
                    IsActive = option.IsActive,
                    SortOrder = option.SortOrder
                }).OrderBy(o => o.SortOrder).ToList();
            }

            if (request.IncludePriceTiers && service.PriceTiers.Any())
            {
                viewModel.PriceTiers = service.PriceTiers.Select(tier => new PriceTierViewModel
                {
                    Id = tier.Id,
                    Name = tier.Name,
                    Description = tier.Description,
                    Price = tier.Price.Amount,
                    Currency = tier.Price.Currency,
                    IsDefault = tier.IsDefault,
                    IsActive = tier.IsActive,
                    SortOrder = tier.SortOrder
                }).OrderBy(t => t.SortOrder).ToList();
            }

            if (request.IncludeProvider)
            {
                var provider = await _providerRepository.GetByIdAsync(service.ProviderId, cancellationToken);
                if (provider != null)
                {
                    viewModel.Provider = new ProviderSummaryViewModel
                    {
                        Id = provider.Id.Value,
                        BusinessName = provider.Profile.BusinessName,
                        Description = provider.Profile.BusinessDescription,
                        Type = provider.ProviderType,
                        LogoUrl = provider.Profile.LogoUrl,
                        City = provider.Address.City,
                        State = provider.Address.State,
                        AllowOnlineBooking = provider.AllowOnlineBooking,
                        OffersMobileServices = provider.OffersMobileServices
                    };
                }
            }

            return viewModel;
        }
    }
}