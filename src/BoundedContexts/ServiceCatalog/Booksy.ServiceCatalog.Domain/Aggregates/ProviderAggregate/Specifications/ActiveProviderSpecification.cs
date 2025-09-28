// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Specifications/ActiveProviderSpecification.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates;

using Provider = Booksy.ServiceCatalog.Domain.Aggregates.Provider;
using System.Linq.Expressions;

namespace Booksy.ServiceCatalog.Domain.Specifications
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

        public static Expression<Func<Aggregates.Provider, bool>> IsActiveWithType(ProviderType type)
        {
            return provider => provider.Status == ProviderStatus.Active &&
                             provider.Type == type;
        }
    }
}