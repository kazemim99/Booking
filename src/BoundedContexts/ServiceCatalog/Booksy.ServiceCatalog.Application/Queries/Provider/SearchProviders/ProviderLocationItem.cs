
namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public sealed class ProviderLocationItem
    {
        public Guid Id { get; init; }
        public string BusinessName { get; init; }
        public string Description { get; init; }
        public ProviderType Type { get; init; }
        public ProviderStatus Status { get; init; }
        public AddressInfo Address { get; init; }
        public CoordinatesInfo Coordinates { get; init; }
        public double DistanceKm { get; init; }
        public string? LogoUrl { get; init; }
        public bool AllowOnlineBooking { get; init; }
        public bool OffersMobileServices { get; init; }
        public decimal AverageRating { get; init; }
        public int ServiceCount { get; init; }

        public ProviderLocationItem(
            Guid id,
            string businessName,
            string description,
            ProviderType type,
            ProviderStatus status,
            AddressInfo address,
            CoordinatesInfo coordinates,
            double distanceKm,
            string? logoUrl,
            bool allowOnlineBooking,
            bool offersMobileServices,
            decimal averageRating,
            int serviceCount)
        {
            Id = id;
            BusinessName = businessName;
            Description = description;
            Type = type;
            Status = status;
            Address = address;
            Coordinates = coordinates;
            DistanceKm = distanceKm;
            LogoUrl = logoUrl;
            AllowOnlineBooking = allowOnlineBooking;
            OffersMobileServices = offersMobileServices;
            AverageRating = averageRating;
            ServiceCount = serviceCount;
        }
    }

}

