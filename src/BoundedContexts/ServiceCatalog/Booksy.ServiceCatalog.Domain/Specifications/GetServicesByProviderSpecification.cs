using Booksy.ServiceCatalog.Domain.Aggregates;

namespace Booksy.ServiceCatalog.Domain.Specifications
{
    public sealed class GetServicesByProviderSpecification : BaseSpecification<Service>
    {
        public GetServicesByProviderSpecification(
            ProviderId providerId,
            ServiceStatus? status = null,
            string? category = null,
            bool includeOptions = false,
            bool includePriceTiers = false,
            bool includeProvider = false)
        {
            // Primary filter - services for this provider
         //   AddCriteria(service => service.ProviderId == providerId);

            // Optional status filter
            if (status.HasValue)
            {
                AddCriteria(service => service.Status == status.Value);
            }

            // Optional category filter
            if (category != null)
            {
                AddCriteria(service => service.Category.Name == category);
            }

            // Include related entities based on requirements
            if (includeOptions)
            {
                AddInclude(service => service.Options);
            }

            if (includePriceTiers)
            {
                AddInclude(service => service.PriceTiers);
            }

            if (includeProvider)
            {
                AddInclude(service => service.Provider);
            }

            // Default ordering by name
            AddOrderBy(service => service.Name);
        }
    }

}