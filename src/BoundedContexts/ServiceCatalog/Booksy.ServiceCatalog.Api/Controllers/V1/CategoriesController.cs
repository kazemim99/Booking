using Booksy.Core.Application.CQRS;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Application.Queries.Category.GetCategoriesWithCounts;
using Booksy.ServiceCatalog.Application.Queries.Provider.SearchProviders;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Enums.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Service categories and category information
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Microsoft.AspNetCore.Authorization.AllowAnonymous] // Public discovery: service categories are browseable without authentication (C1 authz audit)
public class CategoriesController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ISender mediator,
        ILogger<CategoriesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all service categories with provider counts
    /// </summary>
    /// <remarks>
    /// Returns all available service categories with the count of active providers
    /// offering services in each category. Useful for displaying category grids
    /// and navigation.
    ///
    /// This endpoint is public and can be called without authentication.
    /// Results are cached for performance.
    /// </remarks>
    /// <param name="limit">Maximum number of categories to return (default: 25)</param>
    /// <param name="onlyPopular">If true, only returns categories with providers, sorted by count (default: false)</param>
    /// <returns>List of categories with provider counts</returns>
    /// <response code="200">Categories retrieved successfully</response>
    [HttpGet]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(List<CategoryWithCountViewModel>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    public async Task<IActionResult> GetCategories(
        [FromQuery] int limit = 25,
        [FromQuery] bool onlyPopular = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting categories (Limit: {Limit}, OnlyPopular: {OnlyPopular})", limit, onlyPopular);

        var query = new GetCategoriesWithCountsQuery(limit, onlyPopular);
        var result = await _mediator.Send(query, cancellationToken);
   


        _logger.LogInformation("Categories retrieved: {Count} categories", result.Count);

        return Ok(result);
    }

    /// <summary>
    /// Get popular categories with provider counts
    /// </summary>
    /// <remarks>
    /// Returns the most popular service categories sorted by the number of providers
    /// offering services in that category. This is a convenience endpoint that
    /// automatically filters to only categories with active providers.
    /// </remarks>
    /// <param name="limit">Number of popular categories to return (default: 8)</param>
    /// <returns>List of popular categories with provider counts</returns>
    /// <response code="200">Popular categories retrieved successfully</response>
    [HttpGet("popular")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(List<CategoryWithCountViewModel>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    public async Task<IActionResult> GetPopularCategories(
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting popular categories (Limit: {Limit})", limit);

        var query = new GetCategoriesWithCountsQuery(limit, OnlyPopular: true);
        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation("Popular categories retrieved: {Count} categories", result.Count);

        return Ok(result);
    }

    /// <summary>
    /// Get the providers in a single category
    /// </summary>
    /// <remarks>
    /// Accepts either the numeric ServiceCategory id (e.g. <c>1</c>) or its slug
    /// (e.g. <c>hair-salon</c>), so category pages can use readable URLs.
    ///
    /// This is the category-scoped view of provider search and returns the same paginated
    /// shape; use <c>/api/v1/providers/search</c> when you need the other filters as well.
    /// </remarks>
    /// <param name="category">ServiceCategory id or slug</param>
    /// <param name="pageNumber">1-based page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>Paginated providers in the category</returns>
    /// <response code="200">Providers retrieved successfully</response>
    /// <response code="404">No such category</response>
    [HttpGet("{category}/providers")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(PagedResult<ProviderSearchItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvidersInCategory(
        string category,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveCategory(category, out var resolved))
        {
            _logger.LogInformation("Unknown category requested: {Category}", category);
            return NotFound(new { message = $"Unknown service category '{category}'." });
        }

        var query = new SearchProvidersQuery(Category: resolved)
        {
            Pagination = new PaginationRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            }
        };

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Category {Category} returned {Count} providers", resolved, result.Items.Count);

        return Ok(result);
    }

    /// <summary>
    /// Resolves a route value to a ServiceCategory, accepting the numeric id or the slug.
    /// Numeric ids are checked against the declared enum members so an out-of-range number
    /// 404s instead of silently filtering on a category that does not exist.
    /// </summary>
    private static bool TryResolveCategory(string value, out ServiceCategory category)
    {
        if (int.TryParse(value, out var id))
        {
            category = (ServiceCategory)id;
            return category.IsDefinedCategory();
        }

        return ServiceCategoryExtensions.TryParseSlug(value, out category);
    }
}
