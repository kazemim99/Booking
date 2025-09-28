namespace Booksy.ServiceCatalog.Application.Queries.Service.SearchServices
{
    public sealed class ProviderInfo
    {
        public Guid Id { get; init; }
        public string BusinessName { get; init; }
        public string City { get; init; }
        public string State { get; init; }
        public bool AllowOnlineBooking { get; init; }
        public bool OffersMobileServices { get; init; }

        public ProviderInfo(
            Guid id,
            string businessName,
            string city,
            string state,
            bool allowOnlineBooking,
            bool offersMobileServices)
        {
            Id = id;
            BusinessName = businessName;
            City = city;
            State = state;
            AllowOnlineBooking = allowOnlineBooking;
            OffersMobileServices = offersMobileServices;
        }
    }

}

