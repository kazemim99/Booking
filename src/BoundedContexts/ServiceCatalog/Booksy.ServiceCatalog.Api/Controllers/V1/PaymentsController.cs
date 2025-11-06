using Booksy.API.Extensions;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Application.Commands.Payment.CapturePayment;
using Booksy.ServiceCatalog.Application.Commands.Payment.ProcessPayment;
using Booksy.ServiceCatalog.Application.Commands.Payment.RefundPayment;
using Booksy.ServiceCatalog.Application.Queries.Payment.CalculatePricing;
using Booksy.ServiceCatalog.Application.Queries.Payment.GetCustomerPayments;
using Booksy.ServiceCatalog.Application.Queries.Payment.GetPaymentDetails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Manages payment operations for bookings
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ISender mediator,
        ILogger<PaymentsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Process a payment for a booking
    /// </summary>
    /// <param name="request">Payment processing details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment result</returns>
    /// <response code="201">Payment successfully processed</response>
    /// <response code="400">Invalid request data or payment failed</response>
    /// <response code="404">Booking not found</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCurrentUserId();
        if (string.IsNullOrEmpty(customerId))
        {
            return Unauthorized();
        }

        if (!Enum.TryParse<Domain.Enums.PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            throw new DomainValidationException("PaymentMethod", $"Invalid payment method: {request.PaymentMethod}");
        }

        var command = new ProcessPaymentCommand(
            BookingId: request.BookingId,
            CustomerId: Guid.Parse(customerId),
            ProviderId: request.ProviderId,
            Amount: request.Amount,
            Currency: request.Currency,
            Method: paymentMethod,
            PaymentMethodId: request.PaymentMethodId,
            Description: request.Description,
            Metadata: request.Metadata?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value));

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} processed for booking {BookingId}. Status: {Status}",
            result.PaymentId, request.BookingId, result.Status);

        if (!result.IsSuccessful)
        {
            throw new DomainValidationException("Payment", result.ErrorMessage ?? "Payment processing failed");
        }

        var response = new PaymentResponse
        {
            PaymentId = result.PaymentId,
            BookingId = request.BookingId,
            CustomerId = Guid.Parse(customerId),
            ProviderId = request.ProviderId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = result.Status,
            PaymentMethod = request.PaymentMethod,
            PaymentIntentId = result.PaymentIntentId,
            IsSuccessful = result.IsSuccessful,
            ErrorMessage = result.ErrorMessage,
            CreatedAt = DateTime.UtcNow
        };

        return CreatedAtAction(nameof(GetPaymentById), new { id = result.PaymentId }, response);
    }

    /// <summary>
    /// Capture an authorized payment
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <param name="request">Capture details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payment information</returns>
    /// <response code="200">Payment successfully captured</response>
    /// <response code="400">Payment cannot be captured</response>
    /// <response code="404">Payment not found</response>
    [HttpPost("{id}/capture")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CapturePayment(
        Guid id,
        [FromBody] CapturePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CapturePaymentCommand(
            PaymentId: id,
            AmountToCapture: request.Amount);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} captured. Status: {Status}",
            id, result.Status);

        if (!result.IsSuccessful)
        {
            throw new DomainValidationException("PaymentCapture", result.ErrorMessage ?? "Payment capture failed");
        }

        var response = new PaymentResponse
        {
            PaymentId = result.PaymentId,
            BookingId = result.BookingId,
            CustomerId = result.CustomerId,
            ProviderId = result.ProviderId,
            Amount = result.Amount,
            Currency = result.Currency,
            Status = result.Status,
            PaymentMethod = result.PaymentMethod,
            PaymentIntentId = result.PaymentIntentId,
            IsSuccessful = result.IsSuccessful,
            ErrorMessage = result.ErrorMessage,
            CreatedAt = result.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Refund a payment
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <param name="request">Refund details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payment information</returns>
    /// <response code="200">Payment successfully refunded</response>
    /// <response code="400">Payment cannot be refunded</response>
    /// <response code="404">Payment not found</response>
    [HttpPost("{id}/refund")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefundPayment(
        Guid id,
        [FromBody] RefundPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<Domain.Enums.RefundReason>(request.Reason, true, out var refundReason))
        {
            throw new DomainValidationException("RefundReason", $"Invalid refund reason: {request.Reason}");
        }

        var command = new RefundPaymentCommand(
            PaymentId: id,
            RefundAmount: request.Amount ?? 0,
            Reason: refundReason,
            Notes: request.Notes);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} refunded. Amount: {Amount}, Reason: {Reason}",
            id, request.Amount, request.Reason);

        if (!result.IsSuccessful)
        {
            throw new DomainValidationException("PaymentRefund", result.ErrorMessage ?? "Payment refund failed");
        }

        var response = new PaymentResponse
        {
            PaymentId = result.PaymentId,
            BookingId = result.BookingId,
            CustomerId = result.CustomerId,
            ProviderId = result.ProviderId,
            Amount = result.Amount,
            Currency = result.Currency,
            Status = result.Status,
            PaymentMethod = result.PaymentMethod,
            PaymentIntentId = result.PaymentIntentId,
            IsSuccessful = result.IsSuccessful,
            ErrorMessage = result.ErrorMessage,
            CreatedAt = result.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Get payment details by ID
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment details with transaction history</returns>
    /// <response code="200">Payment details retrieved</response>
    /// <response code="404">Payment not found</response>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentDetailsViewModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPaymentDetailsQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            throw new NotFoundException($"Payment with ID {id} was not found");
        }

        return Ok(result);
    }

    /// <summary>
    /// Get customer's payment history
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="status">Filter by payment status (optional)</param>
    /// <param name="startDate">Filter by start date (optional)</param>
    /// <param name="endDate">Filter by end date (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of customer payments</returns>
    /// <response code="200">Payment history retrieved</response>
    [HttpGet("customer/{customerId}")]
    [Authorize]
    [ProducesResponseType(typeof(List<PaymentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerPayments(
        Guid customerId,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCustomerPaymentsQuery(customerId, status, startDate, endDate);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Calculate pricing with taxes, discounts, and fees
    /// </summary>
    /// <param name="request">Pricing calculation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed pricing breakdown</returns>
    /// <response code="200">Pricing calculated successfully</response>
    /// <response code="400">Invalid calculation parameters</response>
    [HttpPost("calculate-pricing")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PricingCalculationViewModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculatePricing(
        [FromBody] CalculatePricingRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = new CalculatePricingQuery(
            BaseAmount: request.BaseAmount,
            Currency: request.Currency,
            TaxPercentage: request.TaxPercentage,
            TaxInclusive: request.TaxInclusive,
            DiscountPercentage: request.DiscountPercentage,
            DiscountAmount: request.DiscountAmount,
            PlatformFeePercentage: request.PlatformFeePercentage,
            DepositPercentage: request.DepositPercentage);

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    private string? GetCurrentUserId()
    {
        return User.GetUserId();
    }
}
