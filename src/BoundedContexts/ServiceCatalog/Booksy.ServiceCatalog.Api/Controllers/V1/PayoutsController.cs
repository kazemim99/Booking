using Booksy.API.Extensions;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Application.Commands.Payout.CreatePayout;
using Booksy.ServiceCatalog.Application.Commands.Payout.ExecutePayout;
using Booksy.ServiceCatalog.Application.Queries.Payout.GetPendingPayouts;
using Booksy.ServiceCatalog.Application.Queries.Payout.GetProviderPayouts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Manages payout operations for providers
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class PayoutsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<PayoutsController> _logger;

    public PayoutsController(
        ISender mediator,
        ILogger<PayoutsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a payout for a provider
    /// </summary>
    /// <param name="request">Payout creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payout information</returns>
    /// <response code="201">Payout successfully created</response>
    /// <response code="400">Invalid request data or no payments found in period</response>
    /// <response code="404">Provider not found</response>
    [HttpPost]
    [Authorize(Roles = "Admin,Finance")]
    [ProducesResponseType(typeof(PayoutResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePayout(
        [FromBody] CreatePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreatePayoutCommand(
            ProviderId: request.ProviderId,
            PeriodStart: request.PeriodStart,
            PeriodEnd: request.PeriodEnd,
            CommissionPercentage: request.CommissionPercentage,
            Notes: request.Notes);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "Payout {PayoutId} created for provider {ProviderId}. Period: {Start} to {End}, Net: {Amount}",
            result.PayoutId, request.ProviderId, request.PeriodStart, request.PeriodEnd, result.NetAmount);

        var response = new PayoutResponse
        {
            PayoutId = result.PayoutId,
            ProviderId = request.ProviderId,
            GrossAmount = result.GrossAmount,
            CommissionAmount = result.CommissionAmount,
            NetAmount = result.NetAmount,
            Currency = result.Currency,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            PaymentCount = result.PaymentCount,
            Status = result.Status,
            IsSuccessful = true,
            CreatedAt = DateTime.UtcNow
        };

        return CreatedAtAction(nameof(GetPayoutById), new { id = result.PayoutId }, response);
    }

    /// <summary>
    /// Execute a payout via payment gateway
    /// </summary>
    /// <param name="id">Payout ID</param>
    /// <param name="request">Execution details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payout information</returns>
    /// <response code="200">Payout successfully executed</response>
    /// <response code="400">Payout cannot be executed or gateway error</response>
    /// <response code="404">Payout not found</response>
    [HttpPost("{id}/execute")]
    [Authorize(Roles = "Admin,Finance")]
    [ProducesResponseType(typeof(PayoutResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExecutePayout(
        Guid id,
        [FromBody] ExecutePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ExecutePayoutCommand(
            PayoutId: id,
            ConnectedAccountId: request.ConnectedAccountId);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "Payout {PayoutId} executed. External ID: {ExternalId}, Status: {Status}",
            id, result.ExternalPayoutId, result.Status);

        if (!result.IsSuccessful)
        {
            throw new DomainValidationException("PayoutExecution", result.ErrorMessage ?? "Failed to execute payout");
        }

        var response = new PayoutResponse
        {
            PayoutId = result.PayoutId,
            ProviderId = result.ProviderId,
            GrossAmount = result.GrossAmount,
            CommissionAmount = result.CommissionAmount,
            NetAmount = result.NetAmount,
            Currency = result.Currency,
            PeriodStart = result.PeriodStart,
            PeriodEnd = result.PeriodEnd,
            PaymentCount = result.PaymentCount,
            Status = result.Status,
            ExternalPayoutId = result.ExternalPayoutId,
            IsSuccessful = result.IsSuccessful,
            ErrorMessage = result.ErrorMessage,
            CreatedAt = result.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Get payout details by ID
    /// </summary>
    /// <param name="id">Payout ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout details</returns>
    /// <response code="200">Payout details retrieved</response>
    /// <response code="404">Payout not found</response>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PayoutDetailsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayoutById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProviderPayoutsQuery(Guid.Empty, null, null, null);
        var allPayouts = await _mediator.Send(query, cancellationToken);
        var result = allPayouts.FirstOrDefault(p => p.PayoutId == id);

        if (result == null)
        {
            throw new NotFoundException($"Payout with ID {id} was not found");
        }

        return Ok(result);
    }

    /// <summary>
    /// Get pending payouts awaiting execution
    /// </summary>
    /// <param name="beforeDate">Only include payouts scheduled before this date (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending payouts</returns>
    /// <response code="200">Pending payouts retrieved</response>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Finance")]
    [ProducesResponseType(typeof(List<PayoutSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingPayouts(
        [FromQuery] DateTime? beforeDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPendingPayoutsQuery(beforeDate);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get provider's payout history
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="status">Filter by payout status (optional)</param>
    /// <param name="startDate">Filter by start date (optional)</param>
    /// <param name="endDate">Filter by end date (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of provider payouts</returns>
    /// <response code="200">Payout history retrieved</response>
    [HttpGet("provider/{providerId}")]
    [Authorize]
    [ProducesResponseType(typeof(List<PayoutDetailsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderPayouts(
        Guid providerId,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProviderPayoutsQuery(providerId, status, startDate, endDate);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }
}
