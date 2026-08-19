// ========================================
// Booksy.ServiceCatalog.Application/Queries/Category/GetCategoriesWithCounts/GetCategoriesWithCountsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Category.GetCategoriesWithCounts
{
    /// <summary>
    /// Handler for getting categories with provider counts
    /// </summary>
    public sealed class GetCategoriesWithCountsQueryHandler
        : IQueryHandler<GetCategoriesWithCountsQuery, List<CategoryWithCountViewModel>>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly ILogger<GetCategoriesWithCountsQueryHandler> _logger;

        public GetCategoriesWithCountsQueryHandler(
            IProviderReadRepository providerRepository,
            ILogger<GetCategoriesWithCountsQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _logger = logger;
        }

        public async Task<List<CategoryWithCountViewModel>> Handle(
            GetCategoriesWithCountsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting categories with provider counts (Limit: {Limit}, OnlyPopular: {OnlyPopular})",
                request.Limit, request.OnlyPopular);

            try
            {
                // Counted with a GROUP BY in the database rather than by materialising every active
                // provider — the browse page must not scale with the size of the provider table.
                var categoryCounts = await _providerRepository.CountByCategoryAsync(
                    ProviderStatus.Active, cancellationToken);

                // The taxonomy is the enum, not whatever happens to be in the database, so every
                // category gets a row and empty ones are flagged rather than dropped.
                var categories = ServiceCategoryExtensions.GetAll().Select(category =>
                {
                    var count = categoryCounts.GetValueOrDefault(category, 0);
                    return new CategoryWithCountViewModel
                    {
                        Id = (int)category,
                        Key = category.ToString(),
                        Name = category.ToPersianName(),
                        EnglishName = category.ToEnglishName(),
                        Slug = category.ToSlug(),
                        Description = category.ToDescription(),
                        Icon = category.ToIcon(),
                        Color = category.ToColorHex(),
                        Gradient = category.ToGradient(),
                        DisplayOrder = (int)category,
                        ProviderCount = count,
                        IsComingSoon = count == 0
                    };
                }).ToList();

                if (request.OnlyPopular)
                {
                    // "Popular" is explicitly the bookable subset: drop empty categories and rank by size.
                    categories = categories
                        .Where(c => c.ProviderCount > 0)
                        .OrderByDescending(c => c.ProviderCount)
                        .ThenBy(c => c.DisplayOrder)
                        .Take(request.Limit)
                        .ToList();
                }
                else
                {
                    // Browsing shows the whole taxonomy in its declared order, empty ones included.
                    categories = categories
                        .OrderBy(c => c.DisplayOrder)
                        .Take(request.Limit)
                        .ToList();
                }

                _logger.LogInformation("Retrieved {Count} categories", categories.Count);

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories with counts");
                throw;
            }
        }
    }
}
