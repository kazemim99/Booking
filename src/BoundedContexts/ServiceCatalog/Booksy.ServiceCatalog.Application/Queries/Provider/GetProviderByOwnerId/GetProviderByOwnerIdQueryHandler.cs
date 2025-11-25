using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetRegistrationProgress;
using Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderByOwnerId
{
    public sealed class GetProviderByOwnerIdQueryHandler : IQueryHandler<GetProviderByOwnerIdQuery, ProviderDetailsResult?>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetProviderByOwnerIdQuery> _logger;

        public GetProviderByOwnerIdQueryHandler(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            ILogger<GetProviderByOwnerIdQuery> logger)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        public async Task<ProviderDetailsResult?> Handle(
            GetProviderByOwnerIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting provider details for owner user ID: {OwnerUserId}", request.OwnerUserId);

            var ownerId = UserId.From(request.OwnerUserId);
            var provider = await _providerRepository.GetByOwnerIdAsync(ownerId, cancellationToken);

            if (provider == null)
            {
                _logger.LogWarning("Provider not found for owner: {OwnerUserId}", request.OwnerUserId);
                return null;
            }

            var viewModel = new ProviderDetailsResult
            {

                Id = provider.Id,
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
                LastActiveAt = provider.LastActiveAt
            };

        
       

            return viewModel;
        }
    }
}