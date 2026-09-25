// ========================================
// AsanRezerve.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Specifications/ActiveProviderSpecification.cs
// ========================================
using AsanRezerve.ServiceCatalog.Domain.Aggregates;

using Provider = AsanRezerve.ServiceCatalog.Domain.Aggregates.Provider;
using System.Linq.Expressions;

namespace AsanRezerve.ServiceCatalog.Domain.Specifications
{
    public sealed class ActiveProviderSpecification
    {
        public static Expression<Func<Aggregates.Provider, bool>> IsSatisfiedBy()
        {
            return provider => provider.Status == ProviderStatus.Active;
        }

        public static Expression<Func<Aggregates.Provider, bool>> IsActiveAndAcceptsBookings()
        {
            return provider => provider.Status == ProviderStatus.Active &&
                             provider.AllowOnlineBooking;
        }

        public static Expression<Func<Aggregates.Provider, bool>> IsActiveWithCategory(ServiceCategory category)
        {
            return provider => provider.Status == ProviderStatus.Active &&
                             provider.PrimaryCategory == category;
        }
    }
}