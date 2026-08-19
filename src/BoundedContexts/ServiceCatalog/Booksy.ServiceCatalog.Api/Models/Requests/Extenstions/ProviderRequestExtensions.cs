using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Exceptions;
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
                // Accepts either spelling the caller used — see SearchProvidersRequest.Latitude.
                UserLatitude: request.EffectiveLatitude,
                UserLongitude: request.EffectiveLongitude,
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
            // `Type` is an OPTIONAL category filter. It was passed straight to Enum.Parse, which throws on null —
            // so every request that did not name a category (i.e. the normal "what is near me" case) failed with
            // HTTP 400 "Required parameter is missing: value". This endpoint is the only one that returns provider
            // coordinates and distance, so nothing could plot providers on a map.
            //
            // Parse only when a value was supplied, and reject an unknown name rather than silently ignoring it:
            // returning everything would look like a successful filter that quietly did nothing.
            ServiceCategory? category = null;
            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                if (!Enum.TryParse<ServiceCategory>(request.Type, ignoreCase: true, out var parsed)
                    || !Enum.IsDefined(typeof(ServiceCategory), parsed))
                {
                    throw new DomainValidationException(
                        nameof(request.Type),
                        $"'{request.Type}' is not a known service category.");
                }

                category = parsed;
            }

            return new GetProvidersByLocationQuery(
                Latitude: request.Latitude,
                Longitude: request.Longitude,
                RadiusKm: request.RadiusKm,
                Category: category,
                OffersMobileServices: request.OffersMobileServices,
                PageNumber: request.PageNumber,
                PageSize: request.PageSize);
        }
    }
}
