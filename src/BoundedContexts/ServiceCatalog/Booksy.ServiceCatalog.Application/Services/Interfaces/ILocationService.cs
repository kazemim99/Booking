// ========================================
// Booksy.ServiceCatalog.Application/Services/Interfaces/ILocationService.cs
// ========================================
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Services.Interfaces
{
    public interface ILocationService
    {
        Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(BusinessAddress address, CancellationToken cancellationToken = default);
        Task<BusinessAddress?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
        Task<double> CalculateDistanceAsync(double lat1, double lng1, double lat2, double lng2, CancellationToken cancellationToken = default);
        Task<bool> ValidateAddressAsync(BusinessAddress address, CancellationToken cancellationToken = default);
        Task<IEnumerable<BusinessAddress>> SearchAddressesAsync(string query, CancellationToken cancellationToken = default);
    }
}