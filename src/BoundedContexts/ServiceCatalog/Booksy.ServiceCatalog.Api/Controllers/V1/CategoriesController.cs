using Booksy.ServiceCatalog.Application.Queries.Category.GetCategoriesWithCounts;
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
}
