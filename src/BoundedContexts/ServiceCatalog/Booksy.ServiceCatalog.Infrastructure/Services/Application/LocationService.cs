// ========================================
// Booksy.ServiceCatalog.Application/Services/Implementations/LocationService.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Services.Implementations
{
    public sealed class LocationService : ILocationService
    {
        private readonly ILogger<LocationService> _logger;

        public LocationService(ILogger<LocationService> logger)
        {
            _logger = logger;
        }

        public async Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(
            BusinessAddress address,
            CancellationToken cancellationToken = default)
        {
            // In a real implementation, this would call a geocoding service like Google Maps API
            _logger.LogInformation("Geocoding address: {Address}", address.ToString());

            // Mock implementation - return null to indicate no coordinates found
            await Task.Delay(100, cancellationToken);

            // For demo purposes, return some mock coordinates based on city
            return address.City.ToLowerInvariant() switch
            {
                "new york" => (40.7128, -74.0060),
                "london" => (51.5074, -0.1278),
                "paris" => (48.8566, 2.3522),
                "tokyo" => (35.6762, 139.6503),
                _ => null
            };
        }

        public async Task<BusinessAddress?> ReverseGeocodeAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Reverse geocoding coordinates: {Latitude}, {Longitude}", latitude, longitude);

            // Mock implementation
            await Task.Delay(100, cancellationToken);

            // Return a mock address
            return BusinessAddress.Create(
                "123 Main Street, Sample City, Sample State",
                "123 Main Street",
                "Sample City",
                "Sample State",
                "12345",
                "Sample Country",
                null, // ProvinceId
                null, // CityId
                latitude,
                longitude);
        }

        public async Task<double> CalculateDistanceAsync(
            double lat1, double lng1, double lat2, double lng2,
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            // Haversine formula
            const double R = 6371; // Earth's radius in km
            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        public async Task<bool> ValidateAddressAsync(
            BusinessAddress address,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating address: {Address}", address.ToString());

            // Mock validation - basic checks
            await Task.Delay(50, cancellationToken);

            return !string.IsNullOrWhiteSpace(address.Street) &&
                   !string.IsNullOrWhiteSpace(address.City) &&
                   !string.IsNullOrWhiteSpace(address.Country);
        }

        public async Task<IEnumerable<BusinessAddress>> SearchAddressesAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Searching addresses for query: {Query}", query);

            await Task.Delay(100, cancellationToken);

            // Return mock search results
            return new List<BusinessAddress>
            {
                BusinessAddress.Create($"{query} Street, City 1, State 1", $"{query} Street", "City 1", "State 1", "12345", "Country 1"),
                BusinessAddress.Create($"{query} Avenue, City 2, State 2", $"{query} Avenue", "City 2", "State 2", "67890", "Country 2")
            };
        }

        private static double ToRadians(double angle) => angle * Math.PI / 180.0;
    }
}