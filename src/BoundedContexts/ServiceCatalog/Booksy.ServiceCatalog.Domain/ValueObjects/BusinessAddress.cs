// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/BusinessAddress.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    public sealed class BusinessAddress : ValueObject
    {
        public BusinessAddress() { }
        public string FormattedAddress { get; }
        public string Street { get; }
        public string City { get; }
        public string State { get; }
        public string PostalCode { get; }
        public string Country { get; }
        public int? ProvinceId { get; }
        public int? CityId { get; }
        public double? Latitude { get; }
        public double? Longitude { get; }

        private BusinessAddress(string formattedAddress, string street, string city, string state, string postalCode, string country, int? provinceId = null, int? cityId = null, double? latitude = null, double? longitude = null)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new ArgumentException("Street cannot be empty", nameof(street));

       

            FormattedAddress = formattedAddress?.Trim() ?? string.Empty;
            Street = street.Trim();
            City = city.Trim();
            State = state?.Trim() ?? string.Empty;
            PostalCode = postalCode?.Trim() ?? string.Empty;
            Country = country.Trim();
            ProvinceId = provinceId;
            CityId = cityId;
            Latitude = latitude;
            Longitude = longitude;
        }

        public static BusinessAddress Create(string formattedAddress, string street, string city, string state, string postalCode, string country, int? provinceId = null, int? cityId = null, double? latitude = null, double? longitude = null)
            => new(formattedAddress, street, city, state, postalCode, country, provinceId, cityId, latitude, longitude);

        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

        public BusinessAddress WithCoordinates(double latitude, double longitude)
            => new(FormattedAddress, Street, City, State, PostalCode, Country, ProvinceId, CityId, latitude, longitude);

        public override string ToString()
        {
            var parts = new List<string> { Street, City };

            if (!string.IsNullOrEmpty(State)) parts.Add(State);
            if (!string.IsNullOrEmpty(PostalCode)) parts.Add(PostalCode);

            parts.Add(Country);

            return string.Join(", ", parts);
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Street.ToLowerInvariant();
            yield return City.ToLowerInvariant();
            yield return State.ToLowerInvariant();
            yield return PostalCode.ToLowerInvariant();
            yield return Country.ToLowerInvariant();
        }
    }
}