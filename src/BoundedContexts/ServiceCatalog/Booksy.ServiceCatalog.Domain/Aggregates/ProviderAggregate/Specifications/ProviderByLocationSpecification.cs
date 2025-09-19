using Booksy.ServiceCatalog.Domain.Aggregates;
using System.Linq.Expressions;

namespace Booksy.ServiceCatalog.Domain.Specifications
{
    public sealed class ProviderByLocationSpecification
    {
        public static Expression<Func<Provider, bool>> WithinRadius(double latitude, double longitude, double radiusKm)
        {
            // Note: This is a simplified distance calculation. In production, you'd use proper geospatial queries
            return provider => provider.Address.Latitude.HasValue &&
                             provider.Address.Longitude.HasValue &&
                             Math.Abs(provider.Address.Latitude.Value - latitude) < (radiusKm / 111.0) &&
                             Math.Abs(provider.Address.Longitude.Value - longitude) < (radiusKm / 111.0);
        }

        public static Expression<Func<Provider, bool>> InCity(string city)
        {
            return provider => provider.Address.City.ToLower() == city.ToLower();
        }

        public static Expression<Func<Provider, bool>> InState(string state)
        {
            return provider => provider.Address.State.ToLower() == state.ToLower();
        }

        public static Expression<Func<Provider, bool>> InCountry(string country)
        {
            return provider => provider.Address.Country.ToLower() == country.ToLower();
        }

        public static Expression<Func<Provider, bool>> OffersMobileServices()
        {
            return provider => provider.OffersMobileServices;
        }
    }
}