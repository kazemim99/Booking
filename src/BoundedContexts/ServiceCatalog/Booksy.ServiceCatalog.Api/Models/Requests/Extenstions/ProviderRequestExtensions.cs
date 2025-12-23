using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByLocation;
using Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders;



namespace Booksy.ServiceCatalog.Api.Models.Requests.Extenstions
{
    /// <summary>
    /// Extension methods for converting Provider API requests to application queries
    /// </summary>
    public static class ProviderRequestExtensions
    {
        /// <summary>
        /// Converts SearchProvidersRequest to SearchProvidersQuery
        /// </summary>
        public static SearchProvidersQuery ToQuery(this SearchProvidersRequest request)
        {
         

            return new SearchProvidersQuery(
                SearchTerm: request.SearchTerm,
                City: request.City,
                State: request.State,
                Country: request.Country,
                AllowsOnlineBooking: request.AllowsOnlineBooking,
                OffersMobileServices: request.OffersMobileServices,
                VerifiedOnly: request.VerifiedOnly,
                MinRating: request.MinRating,
                ServiceCategory: request.ServiceCategory,
                AvailableOn: request.AvailableOn,
                PriceRange: request.PriceRange,
                SortBy: request.SortBy,
                SortDescending: request.SortDescending,
                UserLatitude: request.UserLatitude,
                UserLongitude: request.UserLongitude,
                IncludeInactive: request.IncludeInactive
              )
            {
                Pagination = new PaginationRequest
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                }
            };
        }

        /// <summary>
        /// Converts GetProvidersByLocationRequest to GetProvidersByLocationQuery
        /// </summary>
        public static GetProvidersByLocationQuery ToQuery(this GetProvidersByLocationRequest request)
        {
        

            return new GetProvidersByLocationQuery(
                Latitude: request.Latitude,
                Longitude: request.Longitude,
                RadiusKm: request.RadiusKm,
                Category: Enum.Parse<ServiceCategory>(request.Type),
                OffersMobileServices: request.OffersMobileServices,
                PageNumber: request.PageNumber,
                PageSize: request.PageSize);
        }
    }
}
