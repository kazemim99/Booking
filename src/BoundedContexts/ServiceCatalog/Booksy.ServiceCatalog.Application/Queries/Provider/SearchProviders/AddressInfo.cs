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

        public AddressInfo(
            string street,
            string city,
            string state,
            string postalCode,
            string country,
            double? latitude = null,
            double? longitude = null)
        {
            Street = street;
            City = city;
            State = state;
            PostalCode = postalCode;
            Country = country;
            Latitude = latitude;
            Longitude = longitude;
        }
    }

}
