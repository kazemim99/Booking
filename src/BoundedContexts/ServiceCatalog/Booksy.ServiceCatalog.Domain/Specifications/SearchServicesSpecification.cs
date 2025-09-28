using Booksy.ServiceCatalog.Domain.Aggregates;

namespace Booksy.ServiceCatalog.Domain.Specifications
{
    public sealed class SearchServicesSpecification : BaseSpecification<Service>
    {
        public SearchServicesSpecification(
            string? searchTerm = null,
            string? category = null,
            ServiceType? type = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? maxDurationMinutes = null,
            bool? availableAsMobile = null,
            string? city = null,
            string? state = null,
            ServiceStatus? status = null,
            bool includeProvider = true,
            bool includeOptions = false,
            bool includePriceTiers = false)
        {
            // Text search across multiple fields
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                AddCriteria(service =>
                    service.Name.ToLower().Contains(term) ||
                    service.Description.ToLower().Contains(term));
            }

            // Category filter
            if (category == null)
            {
                AddCriteria(service => service.Category.Name == category);
            }

            // Service type filter
            if (type.HasValue)
            {
                AddCriteria(service => service.Type == type.Value);
            }

            // Price range filters
            if (minPrice.HasValue)
            {
                AddCriteria(service => service.BasePrice.Amount >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                AddCriteria(service => service.BasePrice.Amount <= maxPrice.Value);
            }

            // Duration filter
            if (maxDurationMinutes.HasValue)
            {
                AddCriteria(service => service.Duration.Value <= maxDurationMinutes.Value);
            }

            // Mobile availability filter
            if (availableAsMobile.HasValue)
            {
                AddCriteria(service => service.AvailableAsMobile == availableAsMobile.Value);
            }

            // Location filters (through provider)
            if (!string.IsNullOrWhiteSpace(city))
            {
                var cityLower = city.Trim().ToLower();
                AddCriteria(service => service.Provider.Address.City.ToLower().Contains(cityLower));
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                var stateLower = state.Trim().ToLower();
                AddCriteria(service => service.Provider.Address.State.ToLower().Contains(stateLower));
            }

            // Status filter (default to active services only)
            if (status.HasValue)
            {
                AddCriteria(service => service.Status == status.Value);
            }
            else
            {
                AddCriteria(service => service.Status == ServiceStatus.Active);
            }

            // Include related entities
            if (includeProvider)
            {
                AddInclude(service => service.Provider);
            }

            if (includeOptions)
            {
                AddInclude(service => service.Options);
            }

            if (includePriceTiers)
            {
                AddInclude(service => service.PriceTiers);
            }

            // Default ordering by relevance (name, then price)
            AddOrderBy(service => service.Name);
            AddThenBy(service => service.BasePrice.Amount);
        }
    }

}