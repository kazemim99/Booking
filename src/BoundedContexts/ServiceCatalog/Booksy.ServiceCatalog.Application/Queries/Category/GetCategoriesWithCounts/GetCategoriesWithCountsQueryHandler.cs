// ========================================
// Booksy.ServiceCatalog.Application/Queries/Category/GetCategoriesWithCounts/GetCategoriesWithCountsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
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
        private readonly IServiceReadRepository _serviceRepository;
        private readonly ILogger<GetCategoriesWithCountsQueryHandler> _logger;

        public GetCategoriesWithCountsQueryHandler(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            ILogger<GetCategoriesWithCountsQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
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
                // Get all active providers
                var activeProviders = await _providerRepository.GetByStatusAsync(ProviderStatus.Active, cancellationToken);

                // Count providers by category
                var categoryCountsDict = new Dictionary<string, int>();

                foreach (var provider in activeProviders)
                {
                    var services = await _serviceRepository.GetByProviderIdAsync(provider.Id, cancellationToken);
                    var activeServices = services.ToList();

                    // Get unique categories from this provider's services
                    var providerCategories = activeServices
                        .Select(s => s.Category.Name.ToLowerInvariant())
                        .Distinct();

                    foreach (var category in providerCategories)
                    {
                        if (!categoryCountsDict.ContainsKey(category))
                        {
                            categoryCountsDict[category] = 0;
                        }
                        categoryCountsDict[category]++;
                    }
                }

                // Build view models with actual counts from domain categories
                var categories = ServiceCategory.All.Select(category => new CategoryWithCountViewModel
                {
                    Name = category.Name,
                    Slug = category.Slug,
                    Description = category.Description,
                    Icon = category.IconUrl ?? "📦",
                    Color = category.Color,
                    Gradient = category.Gradient,
                    DisplayOrder = category.DisplayOrder,
                    ProviderCount = categoryCountsDict.GetValueOrDefault(category.Name.ToLowerInvariant(), 0)
                }).ToList();

                // Filter out categories with no providers
                categories = categories.Where(c => c.ProviderCount > 0).ToList();

                // Sort by provider count descending if only popular requested
                if (request.OnlyPopular)
                {
                    categories = categories
                        .OrderByDescending(c => c.ProviderCount)
                        .Take(request.Limit)
                        .ToList();
                }
                else
                {
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
