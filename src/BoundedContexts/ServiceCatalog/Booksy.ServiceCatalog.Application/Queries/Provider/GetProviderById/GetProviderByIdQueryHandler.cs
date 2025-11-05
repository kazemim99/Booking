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

            var providerId = ProviderId.From(request.ProviderId);
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
                ProfileImageUrl = provider.Profile.ProfileImageUrl,
                Status = provider.Status,
                Type = provider.ProviderType,
                ContactInfo = new DTOs.Provider.ContactInfo(
                    provider.ContactInfo.Email?.Value,
                    provider.ContactInfo.PrimaryPhone?.Value,
                    provider.ContactInfo.SecondaryPhone?.Value,
                    provider.ContactInfo.Website),
                Address = new AddressInfo(
                    provider.Address.Street,
                    provider.Address.City,
                    provider.Address.State,
                               provider.Address.CityId,
                    provider.Address.ProvinceId,
                    provider.Address.PostalCode,

                    provider.Address.Country,
                    provider.Address.Latitude,
                    provider.Address.Longitude),
                BusinessHours = provider.BusinessHours.ToDictionary(
                    bh => bh.DayOfWeek,
                    bh => bh.IsOpen ? new BusinessHoursDto
                    {
                        DayOfWeek = bh.DayOfWeek,
                        IsOpen = bh.IsOpen,
                        OpenTime = bh.OpenTime,
                        CloseTime = bh.CloseTime
                    } : null),
                WebsiteUrl = provider.ContactInfo.Website,
                AllowOnlineBooking = provider.AllowOnlineBooking,
                OffersMobileServices = provider.OffersMobileServices,
                IsVerified = provider.VerifiedAt.HasValue,
                AverageRating = provider.AverageRating,
                TotalReviews = 0,
                ServiceCount = provider.Services.Count,
                StaffCount = provider.Staff.Count,
                YearsInBusiness = provider.RegisteredAt.Year > 0
                    ? DateTime.UtcNow.Year - provider.RegisteredAt.Year
                    : 0,
                Tags = provider.Profile.Tags.AsReadOnly(),
                RegisteredAt = provider.RegisteredAt,
                ActivatedAt = provider.ActivatedAt,
                LastActiveAt = provider.LastActiveAt
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