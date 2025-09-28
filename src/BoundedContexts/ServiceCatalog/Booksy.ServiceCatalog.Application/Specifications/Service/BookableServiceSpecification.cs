// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Service/BookableServiceSpecification.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Specifications.Service
{
    /// <summary>
    /// Specification for services that can be booked - Following UserManagement pattern
    /// </summary>
    public sealed class BookableServiceSpecification : BaseSpecification<Domain.Aggregates.Service>
    {
        public BookableServiceSpecification(
            Guid? providerId = null,
            DateTime? requestedDateTime = null,
            bool mobileOnly = false,
            bool locationOnly = false,
            string? category = null)
        {
            // Base criteria for bookable services
            AddCriteria(service =>
                service.Status == ServiceStatus.Active &&
                service.AllowOnlineBooking);

            // Provider filter
            if (providerId.HasValue)
            {
                AddCriteria(service => service.ProviderId.Value == providerId.Value);
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                AddCriteria(service => service.Category.ToString() == category);
            }

            // Mobile/Location availability filters
            if (mobileOnly)
            {
                AddCriteria(service => service.AvailableAsMobile);
            }
            else if (locationOnly)
            {
                AddCriteria(service => service.AvailableAtLocation);
            }

            // Time-based availability (placeholder for complex logic)
            if (requestedDateTime.HasValue)
            {
                var dayOfWeek = requestedDateTime.Value.DayOfWeek;
                // Add criteria for time-based availability
                // This would need to join with Provider business hours in real implementation
            }

            // Include necessary related data

        }

        public static BookableServiceSpecification CreateWithDeposit()
        {
            var spec = new BookableServiceSpecification();
            spec.AddCriteria(service => service.RequiresDeposit);
            return spec;
        }

        public static BookableServiceSpecification CreateMobileServices(Guid? providerId = null)
        {
            return new BookableServiceSpecification(
                providerId: providerId,
                mobileOnly: true);
        }
    }
}
