using AsanRezerve.ServiceCatalog.API.Models.Requests;
using AsanRezerve.ServiceCatalog.Application.Queries.Service.GetServicesByProvider;
using AsanRezerve.ServiceCatalog.Application.Queries.Service.SearchServices;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Api.Models.Requests.Extenstions
{
    /// <summary>
    /// Extension methods for converting API requests to application queries
    /// </summary>
    public static class RequestExtensions
    {
        /// <summary>
        /// Converts SearchServicesRequest to SearchServicesQuery
        /// </summary>
        public static SearchServicesQuery ToQuery(this SearchServicesRequest request)
        {
            // Parse enum values safely
           

            ServiceType? type = null;
            if (!string.IsNullOrEmpty(request.Type) &&
                Enum.TryParse<ServiceType>(request.Type, true, out var parsedType))
            {
                type = parsedType;
            }

            return new SearchServicesQuery(
                SearchTerm: request.SearchTerm,
                Category: request.Category,
                Type: type,
                MinPrice: request.MinPrice,
                MaxPrice: request.MaxPrice,
                MaxDurationMinutes: request.MaxDurationMinutes,
                AvailableAsMobile: request.AvailableAsMobile,
                City: request.City,
                State: request.State);
        }

        /// <summary>
        /// Converts GetServicesByProviderRequest to GetServicesByProviderQuery
        /// </summary>
        public static GetServicesByProviderQuery ToQuery(this GetServicesByProviderRequest request, Guid providerId)
        {
            // Parse enum values safely
            ServiceStatus? status = null;
            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<ServiceStatus>(request.Status, true, out var parsedStatus))
            {
                status = parsedStatus;
            }

         
            return new GetServicesByProviderQuery(
                ProviderId: providerId,
                Status: status,
                Category: request.Category);
        }
    }
}

