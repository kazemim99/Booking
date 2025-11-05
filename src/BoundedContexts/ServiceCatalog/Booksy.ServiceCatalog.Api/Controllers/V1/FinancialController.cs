using Booksy.API.Extensions;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Application.Queries.Payment.GetProviderEarnings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Booksy.API.Middleware.ExceptionHandlingMiddleware;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Manages financial reporting and analytics
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status500InternalServerError)]
public class FinancialController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<FinancialController> _logger;

    public FinancialController(
        ISender mediator,
        ILogger<FinancialController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get provider earnings summary with daily breakdown
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="startDate">Start of the reporting period</param>
    /// <param name="endDate">End of the reporting period</param>
    /// <param name="commissionPercentage">Commission percentage for calculation (defaults to 15%)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Earnings summary with daily breakdown</returns>
    /// <response code="200">Earnings data retrieved successfully</response>
    /// <response code="400">Invalid date range</response>
    /// <response code="404">Provider not found</response>
    [HttpGet("provider/{providerId}/earnings")]
    [Authorize]
    [ProducesResponseType(typeof(ProviderEarningsViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProviderEarnings(
        Guid providerId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] decimal? commissionPercentage = null,
        CancellationToken cancellationToken = default)
    {
        if (startDate >= endDate)
        {
            return BadRequest(new ApiErrorResult(
                "Start date must be before end date",
                "INVALID_DATE_RANGE"));
        }

        var query = new GetProviderEarningsQuery(
            ProviderId: providerId,
            StartDate: startDate,
            EndDate: endDate,
            CommissionPercentage: commissionPercentage);

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Provider {ProviderId} earnings retrieved for period {Start} to {End}. Net: {NetEarnings}",
            providerId, startDate, endDate, result.NetEarnings);

        return Ok(result);
    }

    /// <summary>
    /// Get provider earnings summary for current month
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="commissionPercentage">Commission percentage for calculation (defaults to 15%)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current month earnings summary</returns>
    /// <response code="200">Earnings data retrieved successfully</response>
    [HttpGet("provider/{providerId}/earnings/current-month")]
    [Authorize]
    [ProducesResponseType(typeof(ProviderEarningsViewModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentMonthEarnings(
        Guid providerId,
        [FromQuery] decimal? commissionPercentage = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var query = new GetProviderEarningsQuery(
            ProviderId: providerId,
            StartDate: startDate,
            EndDate: endDate,
            CommissionPercentage: commissionPercentage);

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Provider {ProviderId} current month earnings retrieved. Net: {NetEarnings}",
            providerId, result.NetEarnings);

        return Ok(result);
    }

    /// <summary>
    /// Get provider earnings summary for previous month
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="commissionPercentage">Commission percentage for calculation (defaults to 15%)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Previous month earnings summary</returns>
    /// <response code="200">Earnings data retrieved successfully</response>
    [HttpGet("provider/{providerId}/earnings/previous-month")]
    [Authorize]
    [ProducesResponseType(typeof(ProviderEarningsViewModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreviousMonthEarnings(
        Guid providerId,
        [FromQuery] decimal? commissionPercentage = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var query = new GetProviderEarningsQuery(
            ProviderId: providerId,
            StartDate: startDate,
            EndDate: endDate,
            CommissionPercentage: commissionPercentage);

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Provider {ProviderId} previous month earnings retrieved. Net: {NetEarnings}",
            providerId, result.NetEarnings);

        return Ok(result);
    }
}
