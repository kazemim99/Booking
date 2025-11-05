using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Application.Queries.Booking.GetAvailableSlots;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Booksy.API.Middleware.ExceptionHandlingMiddleware;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Manages availability checking for booking time slots
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status500InternalServerError)]
public class AvailabilityController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<AvailabilityController> _logger;

    public AvailabilityController(
        ISender mediator,
        ILogger<AvailabilityController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets available time slots for a service on a specific date
    /// </summary>
    /// <param name="request">Availability query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available time slots</returns>
    /// <response code="200">Available slots retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Provider or service not found</response>
    [HttpGet("slots")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<AvailableSlotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] GetAvailableSlotsRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate date is not in the past
        if (request.Date.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new ApiErrorResult
            {
                Message = "Cannot check availability for past dates",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var query = new GetAvailableSlotsQuery(
            ProviderId: request.ProviderId,
            ServiceId: request.ServiceId,
            Date: request.Date,
            StaffId: request.StaffId);

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Retrieved {SlotCount} available slots for service {ServiceId} on {Date}",
            result.AvailableSlots.Count, request.ServiceId, request.Date.Date);

        var response = result.AvailableSlots.Select(slot => new AvailableSlotResponse
        {
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            DurationMinutes = slot.DurationMinutes,
            IsAvailable = slot.IsAvailable,
            AvailableStaffId = slot.AvailableStaffId,
            AvailableStaffName = slot.AvailableStaffName
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Checks if a specific time slot is available
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="serviceId">Service ID</param>
    /// <param name="startTime">Desired start time</param>
    /// <param name="staffId">Optional specific staff ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Availability status</returns>
    /// <response code="200">Availability check completed</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpGet("check")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SlotAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckSlotAvailability(
        [FromQuery] Guid providerId,
        [FromQuery] Guid serviceId,
        [FromQuery] DateTime startTime,
        [FromQuery] Guid? staffId = null,
        CancellationToken cancellationToken = default)
    {
        // Validate start time is not in the past
        if (startTime < DateTime.UtcNow)
        {
            return BadRequest(new ApiErrorResult
            {
                Message = "Cannot check availability for past times",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var query = new GetAvailableSlotsQuery(
            ProviderId: providerId,
            ServiceId: serviceId,
            Date: startTime.Date,
            StaffId: staffId);

        var result = await _mediator.Send(query, cancellationToken);

        // Find the specific slot that matches the requested start time
        var requestedSlot = result.AvailableSlots.FirstOrDefault(s => s.StartTime == startTime);

        var response = new SlotAvailabilityResponse
        {
            IsAvailable = requestedSlot?.IsAvailable ?? false,
            StartTime = startTime,
            Message = requestedSlot?.IsAvailable == true
                ? "Time slot is available"
                : "Time slot is not available",
            AvailableStaffId = requestedSlot?.AvailableStaffId,
            AvailableStaffName = requestedSlot?.AvailableStaffName
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets available dates for a service within a date range
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="serviceId">Service ID</param>
    /// <param name="fromDate">Start of date range</param>
    /// <param name="toDate">End of date range (max 30 days from fromDate)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of dates with availability</returns>
    /// <response code="200">Available dates retrieved successfully</response>
    /// <response code="400">Invalid date range</response>
    [HttpGet("dates")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<DateAvailabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAvailableDates(
        [FromQuery] Guid providerId,
        [FromQuery] Guid serviceId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        // Validate date range
        if (fromDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new ApiErrorResult
            {
                Message = "From date cannot be in the past",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        if (toDate < fromDate)
        {
            return BadRequest(new ApiErrorResult
            {
                Message = "To date must be after from date",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        if ((toDate - fromDate).Days > 30)
        {
            return BadRequest(new ApiErrorResult
            {
                Message = "Date range cannot exceed 30 days",
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var availableDates = new List<DateAvailabilityResponse>();

        // Check each date in the range
        for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
        {
            var query = new GetAvailableSlotsQuery(
                ProviderId: providerId,
                ServiceId: serviceId,
                Date: date,
                StaffId: null);

            var result = await _mediator.Send(query, cancellationToken);

            var availableSlotsCount = result.AvailableSlots.Count(s => s.IsAvailable);

            if (availableSlotsCount > 0)
            {
                availableDates.Add(new DateAvailabilityResponse
                {
                    Date = date,
                    AvailableSlotsCount = availableSlotsCount,
                    HasAvailability = true
                });
            }
        }

        _logger.LogInformation(
            "Found {DateCount} dates with availability for service {ServiceId} between {FromDate} and {ToDate}",
            availableDates.Count, serviceId, fromDate.Date, toDate.Date);

        return Ok(availableDates);
    }
}

/// <summary>
/// Response for slot availability check
/// </summary>
public class SlotAvailabilityResponse
{
    public bool IsAvailable { get; set; }
    public DateTime StartTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? AvailableStaffId { get; set; }
    public string? AvailableStaffName { get; set; }
}

/// <summary>
/// Response for date availability
/// </summary>
public class DateAvailabilityResponse
{
    public DateTime Date { get; set; }
    public int AvailableSlotsCount { get; set; }
    public bool HasAvailability { get; set; }
}
