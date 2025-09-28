
namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public sealed class CoordinatesInfo
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }

        public CoordinatesInfo(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }

}
