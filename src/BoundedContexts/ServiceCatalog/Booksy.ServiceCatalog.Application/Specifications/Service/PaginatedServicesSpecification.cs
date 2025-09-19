// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Service/BookableServiceSpecification.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Specifications.Service
{
    /// <summary>
    /// Specification for paginated service filtering with sorting and search - Following UserManagement pattern
    /// </summary>
    public sealed class PaginatedServicesSpecification : BaseSpecification<Domain.Aggregates.Service>
    {
        public PaginatedServicesSpecification(
            ServiceStatus? status = null,
            string? category = null,
            Guid? providerId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? currency = "USD",
            string? searchTerm = null,
            bool? availableAsMobile = null,
            bool? requiresDeposit = null,
            int pageNumber = 1,
            int pageSize = 20,
            string sortBy = "Name",
            bool sortDescending = false)
        {
            // Status filter
            if (status.HasValue)
            {
                AddCriteria(service => service.Status == status.Value);
            }
            else
            {
                // Exclude archived services by default unless specifically requested
                AddCriteria(service => service.Status != ServiceStatus.Archived);
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                AddCriteria(service => service.Category.ToString() == category);
            }

            // Provider filter
            if (providerId.HasValue)
            {
                AddCriteria(service => service.ProviderId.Value == providerId.Value);
            }

            // Price range filters
            if (!string.IsNullOrWhiteSpace(currency))
            {
                AddCriteria(service => service.BasePrice.Currency == currency);
            }

            if (minPrice.HasValue)
            {
                AddCriteria(service => service.BasePrice.Amount >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                AddCriteria(service => service.BasePrice.Amount <= maxPrice.Value);
            }

            // Search term filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                AddCriteria(service =>
                    service.Name.ToLower().Contains(term) ||
                    service.Description.ToLower().Contains(term) ||
                    service.Tags.Any(tag => tag.ToLower().Contains(term)));
            }

            // Mobile availability filter
            if (availableAsMobile.HasValue)
            {
                AddCriteria(service => service.AvailableAsMobile == availableAsMobile.Value);
            }

            // Deposit requirement filter
            if (requiresDeposit.HasValue)
            {
                AddCriteria(service => service.RequiresDeposit == requiresDeposit.Value);
            }

            // Add necessary includes
            AddInclude(s => s.QualifiedStaff);

            ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        }

    }
}

