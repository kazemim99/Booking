using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderAvailabilityCalendar;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Booksy.ServiceCatalog.Api.Controllers.V1;

/// <summary>
/// Provider Availability API
/// Provides availability calendar with time slots and heatmap data for booking UI
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/providers/{providerId}/availability")]
[Produces("application/json")]
public class ProviderAvailabilityController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<ProviderAvailabilityController> _logger;

    public ProviderAvailabilityController(
        ISender mediator,
        ILogger<ProviderAvailabilityController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get provider availability calendar with time slots and heatmap data
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="request">Query parameters (start date, days)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Availability calendar with available/booked time slots and heatmap visualization data</returns>
    /// <remarks>
    /// Returns availability data for the specified provider over a 7, 14, or 30-day window.
    ///
    /// Features:
    /// - Available and booked time slots for each day
    /// - Availability heatmap showing percentages (green/yellow/red color coding)
    /// - Day status indicators (FullyBooked, HighDemand, Moderate, MostlyAvailable, Closed)
    /// - Cached for 5 minutes for optimal performance
    ///
    /// Sample request:
    ///
    ///     GET /api/v1/providers/123e4567-e89b-12d3-a456-426614174000/availability?startDate=2025-11-20&amp;days=7
    ///
    /// Sample response structure:
    ///
    ///     {
    ///       "providerId": "123e4567-e89b-12d3-a456-426614174000",
    ///       "startDate": "2025-11-20",
    ///       "endDate": "2025-11-26",
    ///       "totalDays": 7,
    ///       "days": [
    ///         {
    ///           "date": "2025-11-20",
    ///           "dayOfWeek": "Wednesday",
    ///           "isOpen": true,
    ///           "availableSlots": [
    ///             {
    ///               "startTime": "10:00",
    ///               "endTime": "10:30",
    ///               "durationMinutes": 30,
    ///               "status": "Available",
    ///               "canBook": true
    ///             }
    ///           ],
    ///           "bookedSlots": [ /* ... */ ],
    ///           "status": "Moderate",
    ///           "availableCount": 12,
    ///           "bookedCount": 8
    ///         }
    ///       ],
    ///       "heatmap": {
    ///         "availablePercentage": 45.5,
    ///         "bookedPercentage": 50.0,
    ///         "blockedPercentage": 4.5,
    ///         "totalSlots": 140,
    ///         "availableSlots": 64,
    ///         "bookedSlots": 70,
    ///         "blockedSlots": 6,
    ///         "dailyHeatmap": [
    ///           {
    ///             "date": "2025-11-20",
    ///             "availablePercentage": 60.0,
    ///             "heatmapColor": "yellow"
    ///           }
    ///         ]
    ///       }
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Returns the availability calendar</response>
    /// <response code="400">Invalid request parameters (e.g., invalid date format, days not 7/14/30)</response>
    /// <response code="404">Provider not found</response>
    /// <response code="429">Too many requests (rate limit exceeded)</response>
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("provider-availability")]
    [ProducesResponseType(typeof(ProviderAvailabilityCalendarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetAvailabilityCalendar(
        [FromRoute] Guid providerId,
        [FromQuery] GetProviderAvailabilityCalendarRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting availability calendar for provider {ProviderId}, StartDate: {StartDate}, Days: {Days}",
            providerId,
            request.StartDate ?? "today",
            request.Days);

        // Parse start date or default to today
        DateOnly startDate;
        if (string.IsNullOrWhiteSpace(request.StartDate))
        {
            startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }
        else if (!DateOnly.TryParse(request.StartDate, out startDate))
        {
            _logger.LogWarning("Invalid start date format: {StartDate}", request.StartDate);
            return BadRequest(new ApiErrorResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Invalid start date format. Use yyyy-MM-dd (e.g., 2025-11-20)",
                Errors = new Dictionary<string, string[]>
                {
                    ["StartDate"] = new[] { "Must be in yyyy-MM-dd format" }
                }
            });
        }

        // Validate days parameter
        if (request.Days != 7 && request.Days != 14 && request.Days != 30)
        {
            _logger.LogWarning("Invalid days parameter: {Days}", request.Days);
            return BadRequest(new ApiErrorResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Days parameter must be 7, 14, or 30",
                Errors = new Dictionary<string, string[]>
                {
                    ["Days"] = new[] { "Must be 7, 14, or 30" }
                }
            });
        }

        // Create and send query
        var query = new GetProviderAvailabilityCalendarQuery(
            ProviderId: providerId,
            StartDate: startDate,
            Days: request.Days);

        try
        {
            var viewModel = await _mediator.Send(query, cancellationToken);

            // Map to API response DTO
            var response = MapToResponse(viewModel);

            _logger.LogInformation(
                "Successfully retrieved availability calendar for provider {ProviderId}: {TotalSlots} total slots, {AvailableSlots} available",
                providerId,
                viewModel.Heatmap.TotalSlots,
                viewModel.Heatmap.AvailableSlots);

            return Ok(response);
        }
        catch (Core.Domain.Exceptions.DomainValidationException ex)
        {
            _logger.LogWarning(ex, "Domain validation error for provider {ProviderId}", providerId);
            return NotFound(new ApiErrorResponse
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving availability calendar for provider {ProviderId}", providerId);
            throw;
        }
    }

    private ProviderAvailabilityCalendarResponse MapToResponse(ProviderAvailabilityCalendarViewModel viewModel)
    {
        return new ProviderAvailabilityCalendarResponse
        {
            ProviderId = viewModel.ProviderId,
            StartDate = viewModel.StartDate.ToString("yyyy-MM-dd"),
            EndDate = viewModel.EndDate.ToString("yyyy-MM-dd"),
            TotalDays = viewModel.TotalDays,
            Days = viewModel.Days.Select(d => new DayAvailabilityResponse
            {
                Date = d.Date.ToString("yyyy-MM-dd"),
                DayOfWeek = d.DayOfWeek.ToString(),
                IsOpen = d.IsOpen,
                ClosedReason = d.ClosedReason,
                AvailableSlots = d.AvailableSlots.Select(MapToTimeSlotResponse).ToList(),
                BookedSlots = d.BookedSlots.Select(MapToTimeSlotResponse).ToList(),
                Status = d.Status.ToString(),
                AvailableCount = d.AvailableSlots.Count,
                BookedCount = d.BookedSlots.Count
            }).ToList(),
            Heatmap = new AvailabilityHeatmapResponse
            {
                AvailablePercentage = viewModel.Heatmap.AvailablePercentage,
                BookedPercentage = viewModel.Heatmap.BookedPercentage,
                BlockedPercentage = viewModel.Heatmap.BlockedPercentage,
                TotalSlots = viewModel.Heatmap.TotalSlots,
                AvailableSlots = viewModel.Heatmap.AvailableSlots,
                BookedSlots = viewModel.Heatmap.BookedSlots,
                BlockedSlots = viewModel.Heatmap.BlockedSlots,
                DailyHeatmap = viewModel.Heatmap.DailyHeatmap.Select(dh => new DayHeatmapResponse
                {
                    Date = dh.Date.ToString("yyyy-MM-dd"),
                    AvailablePercentage = dh.AvailablePercentage,
                    HeatmapColor = dh.HeatmapColor
                }).ToList()
            }
        };
    }

    private TimeSlotResponse MapToTimeSlotResponse(TimeSlotViewModel slot)
    {
        return new TimeSlotResponse
        {
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            DurationMinutes = slot.DurationMinutes,
            Status = slot.Status,
            StaffId = slot.StaffId,
            BookingId = slot.BookingId,
            CanBook = slot.CanBook
        };
    }
}
