

using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Application.DTOs.Provider;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Specifications.Provider;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByLocation
{
    public sealed class GetProvidersByLocationQueryHandler : IQueryHandler<GetProvidersByLocationQuery, PagedResult<ProviderLocationItem>>
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

        public async Task<PagedResult<ProviderLocationItem>> Handle(
            GetProvidersByLocationQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Processing providers by location: Lat={Latitude}, Lng={Longitude}, Radius={RadiusKm}",
                request.Latitude, request.Longitude, request.RadiusKm);

            try
            {
                // Create location-based specification
                var specification = new ProvidersByLocationSpecification(
                    latitude: request.Latitude,
                    longitude: request.Longitude,
                    radiusKm: request.RadiusKm,
                    type: request.Type,
                    offersMobileServices: request.OffersMobileServices);


                // Use generic pagination extension
                var result = await _providerRepository.GetPaginatedAsync(
                    specification,
                    request.Pagination,
                    provider => new ProviderLocationItem(
                        provider.Id.Value,
                        provider.Profile.BusinessName,
                        provider.Profile.BusinessDescription,
                        provider.ProviderType,
                       provider.Status,
                      new AddressInfo(
                             provider.Address.Street,
                           provider.Address.City,
                            provider.Address.State,
                            provider.Address.CityId,
                            provider.Address.ProvinceId,
                            provider.Address.PostalCode,
                            provider.Address.Country,
                            provider.Address.Latitude,
                            provider.Address.Longitude),
                       new CoordinatesInfo(
                             provider.Address.Latitude.Value,
                             provider.Address.Longitude.Value),
                        CalculateDistance(
                            request.Latitude, request.Longitude,
                            provider.Address.Latitude.Value, provider.Address.Longitude.Value),
                         provider.Profile.LogoUrl,
                      provider.AllowOnlineBooking,
                        provider.OffersMobileServices,
                        provider.AverageRating,
                         provider.Services.Count),
                    cancellationToken);

                _logger.LogInformation("Location search completed. Found {TotalCount} providers within {RadiusKm}km",
                    result.TotalCount, request.RadiusKm);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during location-based provider search");
                throw;
            }
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for calculating distance between two points
            const double R = 6371; // Earth's radius in kilometers

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double angle) => angle * Math.PI / 180.0;
    }
}
