// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Provider/ProviderByOwnerSpecification.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using System.Linq.Expressions;

namespace Booksy.ServiceCatalog.Application.Specifications.Provider
{
    public sealed class ProviderByOwnerSpecification : ISpecification<Domain.Aggregates.Provider>
    {
        private readonly UserId _ownerId;

        public ProviderByOwnerSpecification(UserId ownerId)
        {
            _ownerId = ownerId;
        }

        public Expression<Func<Domain.Aggregates.Provider, bool>>? Criteria =>
            provider => provider.OwnerId == _ownerId;

        public List<Expression<Func<Domain.Aggregates.Provider, object>>> Includes { get; } = new()
        {
            p => p.Profile,
            p => p.BusinessHours
        };

        public Expression<Func<Domain.Aggregates.Provider, object>>? OrderBy => null;

        public Expression<Func<Domain.Aggregates.Provider, object>>? OrderByDescending =>
            provider => provider.RegisteredAt;

        public int Take { get; set; }

        public int Skip { get; set; }

        public bool IsPagingEnabled => Take > 0;

        public List<string> IncludeStrings => throw new NotImplementedException();
    }
}