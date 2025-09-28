using Booksy.ServiceCatalog.Api.Models.Responses;

namespace Booksy.ServiceCatalog.API.Models.Responses;

public class ProviderLocationResponse : ProviderSummaryResponse
{
    public AddressResponse Address { get; set; } = new();
    public double DistanceKm { get; set; }
    public CoordinatesResponse Coordinates { get; internal set; }
}
