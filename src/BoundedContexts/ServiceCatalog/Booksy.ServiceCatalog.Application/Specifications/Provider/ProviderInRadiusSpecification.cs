// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Provider/ProviderInRadiusSpecification.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Enums;
using System.Linq.Expressions;

namespace Booksy.ServiceCatalog.Application.Specifications.Provider
{
    public sealed class ProviderInRadiusSpecification : ISpecification<Domain.Aggregates.Provider>
    {
        private readonly double _latitude;
        private readonly double _longitude;
        private readonly double _radiusKm;

        public ProviderInRadiusSpecification(double latitude, double longitude, double radiusKm)
        {
            _latitude = latitude;
            _longitude = longitude;
            _radiusKm = radiusKm;
        }

        public Expression<Func<Domain.Aggregates.Provider, bool>>? Criteria =>
            provider => provider.Status == ProviderStatus.Active &&
                       provider.Address.Latitude.HasValue &&
                       provider.Address.Longitude.HasValue &&
                       Math.Abs(provider.Address.Latitude.Value - _latitude) < (_radiusKm / 111.0) &&
                       Math.Abs(provider.Address.Longitude.Value - _longitude) < (_radiusKm / 111.0);

        public List<Expression<Func<Domain.Aggregates.Provider, object>>> Includes { get; } = new()
        {
            p => p.Profile
        };

        public Expression<Func<Domain.Aggregates.Provider, object>>? OrderBy => null;

        public Expression<Func<Domain.Aggregates.Provider, object>>? OrderByDescending => null;

        public int Take { get; set; }

        public int Skip { get; set; }

        public bool IsPagingEnabled => Take > 0;

        public List<string> IncludeStrings => throw new NotImplementedException();

        // Helper method for more accurate distance calculation
        public static double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
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