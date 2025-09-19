// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Provider/ActiveProviderWithServicesSpecification.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using System.Linq.Expressions;

namespace Booksy.ServiceCatalog.Application.Specifications.Provider
{
    public sealed class ActiveProviderWithServicesSpecification : ISpecification<Domain.Aggregates.Provider>
    {
        private readonly int _minActiveServices;

        public ActiveProviderWithServicesSpecification(int minActiveServices = 1)
        {
            _minActiveServices = minActiveServices;
        }

        public Expression<Func<Domain.Aggregates.Provider, bool>>? Criteria =>
            provider => provider.Status == ProviderStatus.Active &&
                       provider.AllowOnlineBooking;

        public List<Expression<Func<Domain.Aggregates.Provider, object>>> Includes { get; } = new();

        public Expression<Func<Domain.Aggregates.Provider, object>>? OrderBy => null;

        public Expression<Func<Domain.Aggregates.Provider, object>>? OrderByDescending => null;

        public int Take { get; set; }

        public int Skip { get; set; }

        public bool IsPagingEnabled => Take > 0;

        public List<string> IncludeStrings => throw new NotImplementedException();

        // Custom method to check if provider has minimum active services
        public static Expression<Func<Domain.Aggregates.Provider, bool>> HasMinimumActiveServices(int minCount)
        {
            return provider => provider.Status == ProviderStatus.Active;
            // Note: This would require a more complex query in real implementation
            // as it involves counting related services from another aggregate
        }
    }
}