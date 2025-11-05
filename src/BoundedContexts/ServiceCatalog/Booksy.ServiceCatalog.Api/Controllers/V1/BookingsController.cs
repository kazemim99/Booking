using Booksy.API.Extensions;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Application.Commands.Booking.CancelBooking;
using Booksy.ServiceCatalog.Application.Commands.Booking.CompleteBooking;
using Booksy.ServiceCatalog.Application.Commands.Booking.ConfirmBooking;
using Booksy.ServiceCatalog.Application.Commands.Booking.CreateBooking;
using Booksy.ServiceCatalog.Application.Commands.Booking.MarkNoShow;
using Booksy.ServiceCatalog.Application.Commands.Booking.RescheduleBooking;
using Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingDetails;
using Booksy.ServiceCatalog.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Booksy.API.Middleware.ExceptionHandlingMiddleware;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Manages booking operations for service appointments
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status500InternalServerError)]
public class BookingsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<BookingsController> _logger;
    private readonly IBookingReadRepository _bookingReadRepository;

    public BookingsController(
        ISender mediator,
        ILogger<BookingsController> logger,
        IBookingReadRepository bookingReadRepository)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bookingReadRepository = bookingReadRepository ?? throw new ArgumentNullException(nameof(bookingReadRepository));
    }

    /// <summary>
    /// Creates a new booking request
    /// </summary>
    /// <param name="request">Booking creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created booking information</returns>
    /// <response code="201">Booking successfully created</response>
    /// <response code="400">Invalid request data or time slot unavailable</response>
    /// <response code="404">Provider, service, or staff not found</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCurrentUserId();
        if (string.IsNullOrEmpty(customerId))
        {
            return Unauthorized();
        }

        var command = new CreateBookingCommand(
            CustomerId: Guid.Parse(customerId),
            ProviderId: request.ProviderId,
            ServiceId: request.ServiceId,
            StaffId: request.StaffId ?? Guid.Empty,
            StartTime: request.StartTime,
            CustomerNotes: request.CustomerNotes);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "Booking {BookingId} created for customer {CustomerId} with provider {ProviderId}",
            result.BookingId, customerId, request.ProviderId);

        var response = new BookingResponse
        {
            Id = result.BookingId,
            CustomerId = result.CustomerId,
            ProviderId = result.ProviderId,
            ServiceId = result.ServiceId,
            StaffId = result.StaffId,
            Status = result.Status,
            StartTime = result.StartTime,
            EndTime = result.EndTime,
            DurationMinutes = result.DurationMinutes,
            TotalPrice = result.TotalPrice,
            Currency = result.Currency,
            PaymentStatus = result.PaymentStatus,
            CreatedAt = result.CreatedAt
        };

        return CreatedAtAction(nameof(GetBookingById), new { id = result.BookingId }, response);
    }

    /// <summary>
    /// Gets a booking by its ID
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Booking details</returns>
    /// <response code="200">Booking found</response>
    /// <response code="404">Booking not found</response>
    /// <response code="403">Not authorized to view this booking</response>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(BookingDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBookingById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBookingDetailsQuery(BookingId: id);
        var result = await _mediator.Send(query, cancellationToken);

        // Authorization check - only customer, provider, or admin can view
        var currentUserId = GetCurrentUserId();
        if (!CanViewBooking(result, currentUserId))
        {
            _logger.LogWarning("User {UserId} attempted to view booking {BookingId} without permission",
                currentUserId, id);
            return Forbid();
        }

        var response = MapToBookingDetailsResponse(result);
        return Ok(response);
    }

    /// <summary>
    /// Gets bookings for the current customer
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="from">Optional start date filter</param>
    /// <param name="to">Optional end date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of customer bookings</returns>
    /// <response code="200">Bookings retrieved successfully</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet("my-bookings")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<BookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyBookings(
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCurrentUserId();
        if (string.IsNullOrEmpty(customerId))
        {
            return Unauthorized();
        }

        var bookings = await _bookingReadRepository.GetByCustomerIdAsync(
            UserId.From(customerId),
            cancellationToken);

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            bookings = bookings.Where(b => b.Status.ToString() == status).ToList();
        }
        if (from.HasValue)
        {
            bookings = bookings.Where(b => b.TimeSlot.StartTime >= from.Value).ToList();
        }
        if (to.HasValue)
        {
            bookings = bookings.Where(b => b.TimeSlot.StartTime <= to.Value).ToList();
        }

        var response = bookings.Select(MapToBookingResponse).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Gets bookings for a specific provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="from">Optional start date filter</param>
    /// <param name="to">Optional end date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of provider bookings</returns>
    /// <response code="200">Bookings retrieved successfully</response>
    /// <response code="403">Not authorized to view provider bookings</response>
    [HttpGet("provider/{providerId:guid}")]
    [Authorize(Policy = "ProviderOrAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProviderBookings(
        [FromRoute] Guid providerId,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageProvider(providerId))
        {
            _logger.LogWarning("User {UserId} attempted to view bookings for provider {ProviderId} without permission",
                GetCurrentUserId(), providerId);
            return Forbid();
        }

        var bookings = await _bookingReadRepository.GetByProviderIdAsync(providerId, cancellationToken);

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            bookings = bookings.Where(b => b.Status.ToString() == status).ToList();
        }
        if (from.HasValue)
        {
            bookings = bookings.Where(b => b.TimeSlot.StartTime >= from.Value).ToList();
        }
        if (to.HasValue)
        {
            bookings = bookings.Where(b => b.TimeSlot.StartTime <= to.Value).ToList();
        }

        var response = bookings.Select(MapToBookingResponse).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Confirms a booking with payment
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Confirmation details with payment information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    /// <response code="200">Booking confirmed successfully</response>
    /// <response code="400">Invalid request or booking cannot be confirmed</response>
    /// <response code="404">Booking not found</response>
    /// <response code="403">Not authorized to confirm this booking</response>
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = "ProviderOrAdmin")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConfirmBooking(
        [FromRoute] Guid id,
        [FromBody] ConfirmBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ConfirmBookingCommand(
            BookingId: id,
            PaymentIntentId: request.PaymentMethodId);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Booking {BookingId} confirmed by user {UserId}", id, GetCurrentUserId());

        return Ok(new MessageResponse($"Booking confirmed successfully. Transaction ID: {result.BookingId}"));
    }

    /// <summary>
    /// Cancels a booking
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Cancellation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message with refund information</returns>
    /// <response code="200">Booking cancelled successfully</response>
    /// <response code="400">Invalid request or booking cannot be cancelled</response>
    /// <response code="404">Booking not found</response>
    /// <response code="403">Not authorized to cancel this booking</response>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelBooking(
        [FromRoute] Guid id,
        [FromBody] CancelBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CancelBookingCommand(
            BookingId: id,
            Reason: request.Reason);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Booking {BookingId} cancelled by user {UserId}. Refund amount: {RefundAmount}",
            id, request.CancelledBy, result.RefundAmount);

        var message = result.RefundAmount > 0
            ? $"Booking cancelled successfully. Refund of {result.RefundAmount} processed."
            : "Booking cancelled successfully.";

        return Ok(new MessageResponse(message));
    }

    /// <summary>
    /// Reschedules a booking to a new time
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Reschedule details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    /// <response code="200">Booking rescheduled successfully</response>
    /// <response code="400">Invalid request or new time slot unavailable</response>
    /// <response code="404">Booking not found</response>
    /// <response code="403">Not authorized to reschedule this booking</response>
    [HttpPost("{id:guid}/reschedule")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RescheduleBooking(
        [FromRoute] Guid id,
        [FromBody] RescheduleBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RescheduleBookingCommand(
            BookingId: id,
            NewStartTime: request.NewStartTime,
            NewStaffId: request.NewStaffId,
            Reason: request.Reason);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "Booking {OldBookingId} rescheduled successfully. New booking: {NewBookingId}",
            id, result.NewBookingId);

        return Ok(new MessageResponse(
            $"Booking rescheduled successfully. New booking ID: {result.NewBookingId}"));
    }

    /// <summary>
    /// Marks a booking as completed
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Completion details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    /// <response code="200">Booking completed successfully</response>
    /// <response code="400">Invalid request or booking cannot be completed</response>
    /// <response code="404">Booking not found</response>
    /// <response code="403">Not authorized to complete this booking</response>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "ProviderOrAdmin")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CompleteBooking(
        [FromRoute] Guid id,
        [FromBody] CompleteBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CompleteBookingCommand(
            BookingId: id,
            StaffNotes: request.CompletionNotes);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Booking {BookingId} completed by user {UserId}", id, GetCurrentUserId());

        return Ok(new MessageResponse("Booking completed successfully"));
    }

    /// <summary>
    /// Marks a booking as no-show
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">No-show details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    /// <response code="200">Booking marked as no-show successfully</response>
    /// <response code="400">Invalid request or booking cannot be marked as no-show</response>
    /// <response code="404">Booking not found</response>
    /// <response code="403">Not authorized to mark this booking as no-show</response>
    [HttpPost("{id:guid}/no-show")]
    [Authorize(Policy = "ProviderOrAdmin")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAsNoShow(
        [FromRoute] Guid id,
        [FromBody] MarkNoShowRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new MarkNoShowCommand(
            BookingId: id,
            Notes: request.Notes);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Booking {BookingId} marked as no-show by user {UserId}", id, GetCurrentUserId());

        return Ok(new MessageResponse("Booking marked as no-show successfully"));
    }

    #region Private Helper Methods

    private string? GetCurrentUserId()
    {
        return User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
    }

    private string? GetCurrentUserProviderId()
    {
        return User.FindFirst("providerId")?.Value;
    }

    private async Task<bool> CanManageProvider(Guid providerId)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return false;

        // Admins can manage any provider
        if (User.IsInRole("Admin") || User.IsInRole("SysAdmin"))
            return true;

        // Provider owners can manage their own provider
        var currentProviderId = GetCurrentUserProviderId();
        if (!string.IsNullOrEmpty(currentProviderId) && currentProviderId == providerId.ToString())
            return true;

        return false;
    }

    private bool CanViewBooking(BookingDetailsViewModel booking, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        // Admin can view all
        if (User.IsInRole("Admin") || User.IsInRole("SysAdmin"))
            return true;

        // Customer can view their own bookings
        if (booking.CustomerId.ToString() == userId)
            return true;

        // Provider can view their bookings
        var currentProviderId = GetCurrentUserProviderId();
        if (!string.IsNullOrEmpty(currentProviderId) && booking.ProviderId.ToString() == currentProviderId)
            return true;

        return false;
    }

    private BookingResponse MapToBookingResponse(dynamic booking)
    {
        return new BookingResponse
        {
            Id = booking.Id.Value,
            CustomerId = booking.CustomerId.Value,
            ProviderId = booking.ProviderId.Value,
            ServiceId = booking.ServiceId.Value,
            StaffId = booking.StaffId,
            Status = booking.Status.ToString(),
            StartTime = booking.TimeSlot.StartTime,
            EndTime = booking.TimeSlot.EndTime,
            DurationMinutes = (int)booking.TimeSlot.Duration.Value,
            TotalPrice = booking.PaymentInfo.TotalAmount.Amount,
            Currency = booking.PaymentInfo.TotalAmount.Currency,
            PaymentStatus = booking.PaymentInfo.Status.ToString(),
            CreatedAt = booking.CreatedAt
        };
    }

    // TODO: Fix this mapping - BookingDetailsViewModel structure doesn't match expected properties
    private BookingDetailsResponse MapToBookingDetailsResponse(BookingDetailsViewModel result)
    {
        return new BookingDetailsResponse
        {
            Id = result.BookingId,
            CustomerId = result.CustomerId,
            ProviderId = result.ProviderId,
            ServiceId = result.ServiceId,
            StaffId = result.StaffId,
            ServiceName = result.ServiceName,
            // ServiceCategory = result.ServiceCategory, // Property doesn't exist
            ProviderBusinessName = result.ProviderName,
            // ProviderCity = result.ProviderCity, // Property doesn't exist
            StaffName = result.StaffName,
            StartTime = result.StartTime,
            EndTime = result.EndTime,
            DurationMinutes = result.DurationMinutes,
            Status = result.Status,
            PaymentStatus = result.PaymentInfo.Status,
            PaymentInfo = new PaymentInfoResponse
            {
                TotalAmount = result.PaymentInfo.TotalAmount,
                Currency = result.Currency,
                DepositAmount = result.PaymentInfo.DepositAmount,
                PaidAmount = result.PaymentInfo.PaidAmount,
                RefundedAmount = result.PaymentInfo.RefundedAmount,
                RemainingAmount = result.PaymentInfo.RemainingAmount,
                PaymentStatus = result.PaymentInfo.Status
            },
            CustomerNotes = result.CustomerNotes,
            StaffNotes = result.StaffNotes,
            // Policy = new BookingPolicyResponse // TODO: Add policy properties to BookingDetailsViewModel
            // {
            //     MinAdvanceBookingHours = result.PolicyMinAdvanceBookingHours,
            //     MaxAdvanceBookingDays = result.PolicyMaxAdvanceBookingDays,
            //     CancellationWindowHours = result.PolicyCancellationWindowHours,
            //     CancellationFeePercentage = result.PolicyCancellationFeePercentage,
            //     AllowRescheduling = result.PolicyAllowRescheduling,
            //     RescheduleWindowHours = result.PolicyRescheduleWindowHours,
            //     RequireDeposit = result.PolicyRequireDeposit,
            //     DepositPercentage = result.PolicyDepositPercentage
            // },
            // CreatedAt = result.CreatedAt, // Use RequestedAt instead
            CreatedAt = result.RequestedAt,
            // LastModifiedAt = result.LastModifiedAt, // Property doesn't exist
            ConfirmedAt = result.ConfirmedAt,
            CompletedAt = result.CompletedAt,
            CancelledAt = result.CancelledAt
        };
    }

    #endregion
}

/// <summary>
/// Simple message response wrapper
/// </summary>
public class MessageResponse
{
    public string Message { get; set; }

    public MessageResponse(string message)
    {
        Message = message;
    }
}
