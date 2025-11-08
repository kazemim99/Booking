using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Application.Queries.Reports.GetBookingStatistics;
using Booksy.ServiceCatalog.Application.Queries.Reports.GetCustomerHistory;
using Booksy.ServiceCatalog.Application.Queries.Reports.GetProviderPerformance;
using Booksy.ServiceCatalog.Application.Queries.Reports.GetRevenueReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Manages booking reports and analytics using the Visitor pattern
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        ISender mediator,
        ILogger<ReportsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get revenue report with optional provider and date filters
    /// </summary>
    /// <param name="providerId">Optional provider ID to filter by</param>
    /// <param name="startDate">Optional start date for the report period</param>
    /// <param name="endDate">Optional end date for the report period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Revenue report with daily breakdown</returns>
    /// <response code="200">Revenue report retrieved successfully</response>
    /// <response code="400">Invalid date range</response>
    [HttpGet("revenue")]
    [Authorize]
    [ProducesResponseType(typeof(RevenueReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] Guid? providerId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (startDate.HasValue && endDate.HasValue && startDate >= endDate)
        {
            throw new DomainValidationException("DateRange", "Start date must be before end date");
        }

        var query = new GetRevenueReportQuery(providerId, startDate, endDate);
        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Revenue report retrieved. Provider: {ProviderId}, TotalRevenue: {TotalRevenue}",
            providerId, result.TotalRevenue);

        return Ok(result);
    }

    /// <summary>
    /// Get booking statistics with optional provider and date filters
    /// </summary>
    /// <param name="providerId">Optional provider ID to filter by</param>
    /// <param name="startDate">Optional start date for the report period</param>
    /// <param name="endDate">Optional end date for the report period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Booking statistics including completion and cancellation rates</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="400">Invalid date range</response>
    [HttpGet("statistics")]
    [Authorize]
    [ProducesResponseType(typeof(BookingStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookingStatistics(
        [FromQuery] Guid? providerId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (startDate.HasValue && endDate.HasValue && startDate >= endDate)
        {
            throw new DomainValidationException("DateRange", "Start date must be before end date");
        }

        var query = new GetBookingStatisticsQuery(providerId, startDate, endDate);
        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Booking statistics retrieved. Provider: {ProviderId}, Total: {Total}, CompletionRate: {CompletionRate}",
            providerId, result.Total, result.CompletionRate);

        return Ok(result);
    }

    /// <summary>
    /// Get customer booking history and lifetime value
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer history including total spent and favorite providers</returns>
    /// <response code="200">Customer history retrieved successfully</response>
    /// <response code="404">Customer not found</response>
    [HttpGet("customer/{customerId}/history")]
    [Authorize]
    [ProducesResponseType(typeof(CustomerHistoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerHistory(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCustomerHistoryQuery(customerId);
        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Customer {CustomerId} history retrieved. Total bookings: {TotalBookings}, Total spent: {TotalSpent}",
            customerId, result.TotalBookings, result.TotalSpent);

        return Ok(result);
    }

    /// <summary>
    /// Get provider performance metrics and analytics
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="startDate">Optional start date for the report period</param>
    /// <param name="endDate">Optional end date for the report period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider performance including revenue, completion rate, and top services</returns>
    /// <response code="200">Performance metrics retrieved successfully</response>
    /// <response code="400">Invalid date range</response>
    /// <response code="404">Provider not found</response>
    [HttpGet("provider/{providerId}/performance")]
    [Authorize]
    [ProducesResponseType(typeof(ProviderPerformanceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderPerformance(
        Guid providerId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (startDate.HasValue && endDate.HasValue && startDate >= endDate)
        {
            throw new DomainValidationException("DateRange", "Start date must be before end date");
        }

        var query = new GetProviderPerformanceQuery(providerId, startDate, endDate);
        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Provider {ProviderId} performance retrieved. Total bookings: {TotalBookings}, Revenue: {Revenue}, CompletionRate: {CompletionRate}",
            providerId, result.TotalBookings, result.TotalRevenue, result.CompletionRate);

        return Ok(result);
    }
}
