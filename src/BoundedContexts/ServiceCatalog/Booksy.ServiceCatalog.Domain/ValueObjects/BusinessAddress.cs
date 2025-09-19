// ========================================
// Booksy.ServiceCatalog.Domain/ValueObjects/BusinessAddress.cs
// ========================================
using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.ValueObjects
{
    public sealed class BusinessAddress : ValueObject
    {
        public BusinessAddress() { }
        public string Street { get; }
        public string City { get; }
        public string State { get; }
        public string PostalCode { get; }
        public string Country { get; }
        public double? Latitude { get; }
        public double? Longitude { get; }

        private BusinessAddress(string street, string city, string state, string postalCode, string country, double? latitude = null, double? longitude = null)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new ArgumentException("Street cannot be empty", nameof(street));

            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City cannot be empty", nameof(city));

            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Country cannot be empty", nameof(country));

            Street = street.Trim();
            City = city.Trim();
            State = state?.Trim() ?? string.Empty;
            PostalCode = postalCode?.Trim() ?? string.Empty;
            Country = country.Trim();
            Latitude = latitude;
            Longitude = longitude;
        }

        public static BusinessAddress Create(string street, string city, string state, string postalCode, string country, double? latitude = null, double? longitude = null)
            => new(street, city, state, postalCode, country, latitude, longitude);

        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

        public BusinessAddress WithCoordinates(double latitude, double longitude)
            => new(Street, City, State, PostalCode, Country, latitude, longitude);

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