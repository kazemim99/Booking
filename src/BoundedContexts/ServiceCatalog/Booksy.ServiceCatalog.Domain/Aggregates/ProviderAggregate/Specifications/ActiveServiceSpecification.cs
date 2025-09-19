// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ServiceAggregate/Specifications/ActiveServiceSpecification.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates;
using System.Linq.Expressions;

namespace Booksy.ServiceCatalog.Domain.Specifications
{
    public sealed class ActiveServiceSpecification
    {
        public static Expression<Func<Service, bool>> IsSatisfiedBy()
        {
            return service => service.Status == ServiceStatus.Active;
        }

        public static Expression<Func<Service, bool>> IsActiveAndBookable()
        {
            return service => service.Status == ServiceStatus.Active &&
                            service.AllowOnlineBooking &&
                            service.QualifiedStaff.Any();
        }

        public static Expression<Func<Service, bool>> IsActiveInCategory(ServiceCategory category)
        {
            return service => service.Status == ServiceStatus.Active &&
                            service.Category == category;
        }

        public static Expression<Func<Service, bool>> IsActiveWithPriceRange(decimal minPrice, decimal maxPrice, string currency)
        {
            return service => service.Status == ServiceStatus.Active &&
                            service.BasePrice.Currency == currency &&
                            service.BasePrice.Amount >= minPrice &&
                            service.BasePrice.Amount <= maxPrice;
        }

        public static Expression<Func<Service, bool>> IsActiveWithMaxDuration(int maxMinutes)
        {
            return service => service.Status == ServiceStatus.Active &&
                            service.Duration.Value <= maxMinutes;
        }

        public static Expression<Func<Service, bool>> IsAvailableAsMobile()
        {
            return service => service.Status == ServiceStatus.Active &&
                            service.AvailableAsMobile;
        }

        public static Expression<Func<Service, bool>> RequiresDeposit()
        {
            return service => service.Status == ServiceStatus.Active &&
                            service.RequiresDeposit;
        }
    }
}