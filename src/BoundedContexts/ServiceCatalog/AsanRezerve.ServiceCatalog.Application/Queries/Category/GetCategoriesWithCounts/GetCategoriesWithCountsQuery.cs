// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Category/GetCategoriesWithCounts/GetCategoriesWithCountsQuery.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.Caching;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Category.GetCategoriesWithCounts
{
    /// <summary>
    /// Query to get all service categories with provider counts
    /// </summary>
    /// <remarks>Cached for ten minutes (the customer home screen); evicted by any provider change.</remarks>
    public sealed record GetCategoriesWithCountsQuery(
        int Limit = 25,
        bool OnlyPopular = false
    ) : IQuery<List<CategoryWithCountViewModel>>
    {
        public bool IsCacheable => true;

        public int? CacheExpirationSeconds => 600;

        public IReadOnlyCollection<string>? CacheTags => [ReadModelCacheTags.Categories];
    }
}
