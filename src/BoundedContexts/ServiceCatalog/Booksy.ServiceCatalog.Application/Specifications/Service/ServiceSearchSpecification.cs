

namespace Booksy.ServiceCatalog.Application.Specifications.Service
{
    /// <summary>
    /// Specification for service search functionality - Following UserManagement pattern
    /// </summary>
    public sealed class ServiceSearchSpecification : BaseSpecification<Domain.Aggregates.Service>
    {
        public ServiceSearchSpecification(
            string searchTerm,
            string? category = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? maxDuration = null,
            bool? availableAsMobile = null,
            List<string>? tags = null,
            string currency = "USD")
        {
            // Base criteria - only active services for search
            AddCriteria(service => service.Status == ServiceStatus.Active);

            // Search term - comprehensive text search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                AddCriteria(service =>
                    service.Name.ToLower().Contains(term) ||
                    service.Description.ToLower().Contains(term) ||
                    service.Tags.Any(tag => tag.ToLower().Contains(term)));
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                AddCriteria(service => service.Category.ToString() == category);
            }

            // Price filters
            AddCriteria(service => service.BasePrice.Currency == currency);

            if (minPrice.HasValue)
            {
                AddCriteria(service => service.BasePrice.Amount >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                AddCriteria(service => service.BasePrice.Amount <= maxPrice.Value);
            }

            // Duration filter
            if (maxDuration.HasValue)
            {
                AddCriteria(service => service.Duration.Value <= maxDuration.Value);
            }

            // Mobile availability filter
            if (availableAsMobile.HasValue)
            {
                AddCriteria(service => service.AvailableAsMobile == availableAsMobile.Value);
            }

            // Tag filters
            if (tags != null && tags.Any())
            {
                AddCriteria(service => tags.Any(tag =>
                    service.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
            }

            // Include qualified staff for bookability check
            AddInclude(s => s.QualifiedStaff);
        }
    }
}