using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByLocation
{
    public sealed class GetProvidersByLocationQueryHandler : IQueryHandler<GetProvidersByLocationQuery, IReadOnlyList<ProviderLocationViewModel>>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly ILogger<GetProvidersByLocationQueryHandler> _logger;

        public GetProvidersByLocationQueryHandler(
            IProviderReadRepository providerRepository,
            ILogger<GetProvidersByLocationQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ProviderLocationViewModel>> Handle(
            GetProvidersByLocationQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Getting providers by location: Lat={Latitude}, Lng={Longitude}, Radius={RadiusKm}km",
                request.Latitude, request.Longitude, request.RadiusKm);

            var providers = await _providerRepository.GetByLocationAsync(
                request.Latitude,
                request.Longitude,
                request.RadiusKm,
                cancellationToken);

            var filteredProviders = providers.Where(provider =>
                (request.ProviderType == null || provider.Type == request.ProviderType) &&
                (request.OffersMobileServices == null || provider.OffersMobileServices == request.OffersMobileServices));

            var result = filteredProviders
                .Take(request.MaxResults ?? 50)
                .Select(provider => new ProviderLocationViewModel
                {
                    Id = provider.Id.Value,
                    BusinessName = provider.Profile.BusinessName,
                    Description = provider.Profile.Description,
                    Type = provider.Type,
                    LogoUrl = provider.Profile.LogoUrl,
                    Address = new AddressViewModel
                    {
                        Street = provider.Address.Street,
                        City = provider.Address.City,
                        State = provider.Address.State,
                        Country = provider.Address.Country,
                        Latitude = provider.Address.Latitude,
                        Longitude = provider.Address.Longitude
                    },
                    Email = provider.ContactInfo.Email.Value,
                    PrimaryPhone = provider.ContactInfo.PrimaryPhone.Value,
                    Website = provider.ContactInfo.Website,
                    AllowOnlineBooking = provider.AllowOnlineBooking,
                    OffersMobileServices = provider.OffersMobileServices,
                    Tags = provider.Profile.Tags,
                    Distance = CalculateDistance(
                        request.Latitude, request.Longitude,
                        provider.Address.Latitude ?? 0, provider.Address.Longitude ?? 0)
                })
                .OrderBy(p => p.Distance)
                .ToList();

            return result;
        }

        private static double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            // Haversine formula for calculating distance between two points
            const double R = 6371; // Earth's radius in km
            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double angle) => angle * Math.PI / 180.0;
    }
}