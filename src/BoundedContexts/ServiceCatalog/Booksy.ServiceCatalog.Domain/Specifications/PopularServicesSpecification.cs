using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;



namespace Booksy.ServiceCatalog.Domain.Specifications
{
    public sealed class PopularServicesSpecification : BaseSpecification<Service>
    {
        public PopularServicesSpecification(string? categoryFilter = null)
        {
            // Only active services
            AddCriteria(service => service.Status == ServiceStatus.Active);

            // Optional category filter
            if (!string.IsNullOrWhiteSpace(categoryFilter))
            {
                var categoryLower = categoryFilter.Trim().ToLower();
                AddCriteria(service => service.Category.Name.ToLower().Contains(categoryLower));
            }

            // Order by popularity metrics (this would typically include booking count, ratings, etc.)
            // For now, we'll order by creation date as a placeholder
            AddOrderByDescending(service => service.CreatedAt);
            AddThenBy(service => service.Name);
        }
    }
}