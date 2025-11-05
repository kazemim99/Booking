//===========================================
// Provider Query Definitions
//===========================================

//===========================================
// Queries/Provider/SearchProviders/SearchProvidersQuery.cs
//===========================================
namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public sealed class AddressInfo
    {
        public string Street { get; init; }
        public string City { get; init; }
        public string State { get; init; }
        public string PostalCode { get; init; }
        public string Country { get; init; }
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
        public string FormattedAddress { get; set; }
        public int? ProvinceId { get; set; }
        public int? CityId { get; set; }

        public AddressInfo(
            string street,
            string city,
            string state,
            int? cityId,
            int? provinceId,
            string postalCode,
            string country,
            double? latitude = null,
            double? longitude = null)
        {
            Street = street;
            City = city;
            State = state;
            CityId = cityId;
            ProvinceId = provinceId;
            PostalCode = postalCode;
            Country = country;
            Latitude = latitude;
            Longitude = longitude;
        }
    }

}
