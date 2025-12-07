// ========================================
// Booksy.ServiceCatalog.Application/Queries/Category/GetCategoriesWithCounts/GetCategoriesWithCountsQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Category.GetCategoriesWithCounts
{
    /// <summary>
    /// Query to get all service categories with provider counts
    /// </summary>
    public sealed record GetCategoriesWithCountsQuery(
        int Limit = 25,
        bool OnlyPopular = false
    ) : IQuery<List<CategoryWithCountViewModel>>;
}
