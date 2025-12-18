using Booksy.ServiceCatalog.Application.Queries.Platform.GetPlatformStatistics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Platform-wide statistics and information
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class PlatformController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<PlatformController> _logger;

    public PlatformController(
        ISender mediator,
        ILogger<PlatformController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get platform-wide statistics for landing page
    /// </summary>
    /// <remarks>
    /// Returns aggregated statistics across the entire platform including:
    /// - Total number of active providers
    /// - Total customers (estimated)
    /// - Total bookings completed
    /// - Average provider rating
    /// - Number of cities with active providers
    /// - Popular service categories
    ///
    /// This endpoint is public and can be called without authentication.
    /// Results are cached for performance.
    /// </remarks>
    /// <returns>Platform statistics</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    [HttpGet("statistics")]
    [EnableRateLimiting("public-api")]
    [ProducesResponseType(typeof(PlatformStatisticsViewModel), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting platform statistics");

        var query = new GetPlatformStatisticsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Platform statistics retrieved: {Providers} providers, {Services} services",
            result.TotalProviders, result.TotalServices);

        return Ok(result);
    }
}
