// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Service/BookableServiceSpecification.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Specifications.Service
{
    /// <summary>
    /// Specification for services with availability constraints - Following UserManagement pattern
    /// </summary>
    public sealed class ServiceWithAvailabilitySpecification : BaseSpecification<Domain.Aggregates.Service>
    {
        public ServiceWithAvailabilitySpecification(
            DateTime? requestedDateTime = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            Guid? staffId = null,
            int? maxDurationMinutes = null,
            bool advanceBookingCheck = false)
        {
            // Base availability criteria
            AddCriteria(service =>
                service.Status == ServiceStatus.Active &&
                service.AllowOnlineBooking);

            // Duration filter
            if (maxDurationMinutes.HasValue)
            {
                AddCriteria(service => service.Duration.Value <= maxDurationMinutes.Value);
            }

        
            // Advance booking limits check
            if (advanceBookingCheck && requestedDateTime.HasValue)
            {
                var now = DateTime.UtcNow;
                var hoursUntilService = (requestedDateTime.Value - now).TotalHours;
                var daysUntilService = (requestedDateTime.Value.Date - now.Date).TotalDays;

                AddCriteria(service =>
                    hoursUntilService >= service.MinAdvanceBookingHours &&
                    daysUntilService <= service.MaxAdvanceBookingDays);
            }

            // Include qualified staff for evaluation
      
        }

        public static ServiceWithAvailabilitySpecification CreateForDateTime(DateTime requestedDateTime)
        {
            return new ServiceWithAvailabilitySpecification(
                requestedDateTime: requestedDateTime,
                advanceBookingCheck: true);
        }

        public static ServiceWithAvailabilitySpecification CreateForDateRange(DateTime startDate, DateTime endDate)
        {
            return new ServiceWithAvailabilitySpecification(
                startDate: startDate,
                endDate: endDate);
        }

        public static ServiceWithAvailabilitySpecification CreateMobileAvailable(int? maxDuration = null)
        {
            var spec = new ServiceWithAvailabilitySpecification(maxDurationMinutes: maxDuration);
            spec.AddCriteria(service => service.AvailableAsMobile);
            return spec;
        }

        public static ServiceWithAvailabilitySpecification CreateLocationAvailable(int? maxDuration = null)
        {
            var spec = new ServiceWithAvailabilitySpecification(maxDurationMinutes: maxDuration);
            spec.AddCriteria(service => service.AvailableAtLocation);
            return spec;
        }
    }
}
