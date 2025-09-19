// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ServiceAggregate/Specifications/ServiceByProviderSpecification.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates;
using System.Linq.Expressions;

namespace Booksy.ServiceCatalog.Domain.Specifications
{
    public sealed class ServiceByProviderSpecification
    {
        public static Expression<Func<Service, bool>> BelongsToProvider(ProviderId providerId)
        {
            return service => service.ProviderId == providerId;
        }

        public static Expression<Func<Service, bool>> BelongsToProviderAndActive(ProviderId providerId)
        {
            return service => service.ProviderId == providerId &&
                            service.Status == ServiceStatus.Active;
        }

        public static Expression<Func<Service, bool>> BelongsToProviderWithType(ProviderId providerId, ServiceType serviceType)
        {
            return service => service.ProviderId == providerId &&
                            service.Type == serviceType;
        }

        public static Expression<Func<Service, bool>> BelongsToProviderInCategory(ProviderId providerId, ServiceCategory category)
        {
            return service => service.ProviderId == providerId &&
                            service.Category == category;
        }

        public static Expression<Func<Service, bool>> BelongsToProviderAndBookable(ProviderId providerId)
        {
            return service => service.ProviderId == providerId &&
                            service.Status == ServiceStatus.Active &&
                            service.AllowOnlineBooking &&
                            service.QualifiedStaff.Any();
        }
    }
}