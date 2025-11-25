using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetRegistrationProgress;
using Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById
{
    public sealed class GetProviderByIdQueryHandler : IQueryHandler<GetProviderByIdQuery, ProviderDetailsResult?>
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

        public async Task<ProviderDetailsResult?> Handle(
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

            var viewModel = new ProviderDetailsResult
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
                BusinessHours = provider.BusinessHours.Select(bh => new BusinessHoursData(
            DayOfWeek: (int)bh.DayOfWeek,
            IsOpen: bh.IsOpen,
            OpenTimeHours: bh.OpenTime?.Hour,
            OpenTimeMinutes: bh.OpenTime?.Minute,
            CloseTimeHours: bh.CloseTime?.Hour,
            CloseTimeMinutes: bh.CloseTime?.Minute,
            Breaks: bh.Breaks.Select(br => new BreakPeriodData(
                StartTimeHours: br.StartTime.Hour,
                StartTimeMinutes: br.StartTime.Minute,
                EndTimeHours: br.EndTime.Hour,
                EndTimeMinutes: br.EndTime.Minute,
                Label: br.Label
            )).ToList()
        )),
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
                LastActiveAt = provider.LastActiveAt,
                // Hierarchy information
                HierarchyType = provider.HierarchyType,
                IsIndependent = provider.IsIndependent,
                ParentProviderId = provider.ParentProviderId?.Value
            };

            // Fetch parent provider info if this is a linked individual
            if (provider.ParentProviderId != null)
            {
                var parentProvider = await _providerRepository.GetByIdAsync(provider.ParentProviderId, cancellationToken);
                if (parentProvider != null)
                {
                    viewModel.ParentProvider = new ParentProviderInfo
                    {
                        Id = parentProvider.Id.Value,
                        BusinessName = parentProvider.Profile.BusinessName,
                        ProfileImageUrl = parentProvider.Profile.ProfileImageUrl,
                        Status = parentProvider.Status
                    };
                }
            }

            // Fetch staff providers if this is an organization
            if (provider.HierarchyType == Domain.Enums.ProviderHierarchyType.Organization)
            {
                var staffProviders = await _providerRepository.GetStaffByOrganizationIdAsync(providerId, cancellationToken);
                viewModel.StaffProviderCount = staffProviders.Count;
                viewModel.StaffProviders = staffProviders.Select(sp => new StaffProviderInfo
                {
                    Id = sp.Id.Value,
                    BusinessName = sp.Profile.BusinessName,
                    ProfileImageUrl = sp.Profile.ProfileImageUrl,
                    Status = sp.Status,
                    IsIndependent = sp.IsIndependent,
                    AverageRating = sp.AverageRating,
                    ServiceCount = sp.Services.Count
                }).ToList();
            }

            if (request.IncludeStaff)
            {
                viewModel.Staff = provider.Staff.Select(staff => new ProviderStaffItem
                {
                    Id = staff.Id,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    FullName = staff.FullName,
                    Email = staff.Email?.Value,
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
                viewModel.Services = services.Select(service => new ProviderServiceItem
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