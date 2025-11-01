using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessInfo;
using Booksy.ServiceCatalog.Application.Commands.Provider.UpdateLocation;
using Booksy.ServiceCatalog.Application.Commands.Provider.UpdateWorkingHours;
using Booksy.ServiceCatalog.Application.Commands.Service.UpdateProviderService;
using Booksy.ServiceCatalog.Application.Commands.Service.DeleteProviderService;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById;
using Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessHours;
using Booksy.ServiceCatalog.Application.Commands.Provider.AddHoliday;
using Booksy.ServiceCatalog.Application.Commands.Provider.DeleteHoliday;
using Booksy.ServiceCatalog.Application.Commands.Provider.AddException;
using Booksy.ServiceCatalog.Application.Commands.Provider.DeleteException;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetBusinessHours;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetHolidays;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetExceptions;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetAvailability;
using Booksy.ServiceCatalog.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Booksy.API.Middleware.ExceptionHandlingMiddleware;
using Booksy.ServiceCatalog.Application.Commands.Service.AddProviderService;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Provider settings management - allows providers to update their profile sections independently
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/providers")]
[Produces("application/json")]
[Authorize]
[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class ProviderSettingsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IServiceReadRepository _serviceReadRepository;
    private readonly ILogger<ProviderSettingsController> _logger;

    public ProviderSettingsController(
        ISender mediator,
        IServiceReadRepository serviceReadRepository,
        ILogger<ProviderSettingsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _serviceReadRepository = serviceReadRepository ?? throw new ArgumentNullException(nameof(serviceReadRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Business Info


    /// <summary>
    /// Get provider business information
    /// </summary>
    [HttpGet("{id:guid}/business-info")]
    [ProducesResponseType(typeof(BusinessInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBusinessInfo(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        // Authorization check
      

        var query = new GetProviderByIdQuery(id, false, false);
        var provider = await _mediator.Send(query, cancellationToken);

        if (provider == null)
        {
            return NotFound();
        }

        var response = new BusinessInfoResponse
        {
            BusinessName = provider.BusinessName ?? "",
            Description = provider.Description ?? "",
            PhoneNumber = provider.ContactInfo?.Phone ?? "",
            Email = provider.ContactInfo?.Email ?? "",
            Website = provider.ContactInfo?.Website
        };

        return Ok(response);
    }

    /// <summary>
    /// Update provider business information
    /// </summary>
    [HttpPut("{id:guid}/business-info")]
    [ProducesResponseType(typeof(BusinessInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBusinessInfo(
        [FromRoute] Guid id,
        [FromBody] UpdateBusinessInfoRequest request,
        CancellationToken cancellationToken = default)
    {
       
        var command = new UpdateBusinessInfoCommand(
            id,
            request.BusinessName,
            request.Description,
            request.OwnerFirstName,
            request.OwnerLastName,
            request.PhoneNumber,
            request.Email,
            request.Website);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Business info updated for provider {ProviderId}", id);

        var response = new BusinessInfoResponse
        {

            BusinessName = result.BusinessName,
            Description = request.Description,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Website = request.Website
        };

        return Ok(response);
    }

    #endregion

    #region Location

    /// <summary>
    /// Get provider location information
    /// </summary>
    [HttpGet("{id:guid}/location")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLocation(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
      

        var query = new GetProviderByIdQuery(id, false, false);
        var provider = await _mediator.Send(query, cancellationToken);

        if (provider == null)
        {
            return NotFound();
        }

        var response = new LocationResponse
        {
            AddressLine1 = provider.Address?.Street ?? "",
            Street = provider.Address?.Street ?? "",
            City = provider.Address?.City ?? "",
            State = provider.Address?.State,
            PostalCode = provider.Address?.PostalCode ?? "",
            Country = provider.Address?.Country ?? "Iran",
            Latitude = provider.Address?.Latitude,
            Longitude = provider.Address?.Longitude
        };

        return Ok(response);
    }

    /// <summary>
    /// Update provider location
    /// </summary>
    [HttpPut("{id:guid}/location")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateLocation(
        [FromRoute] Guid id,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
      

        var command = new UpdateLocationCommand(
            id,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.State,
            request.PostalCode,
            request.Country,
            request.Latitude,
            request.Longitude,
            request.FormattedAddress,
            request.IsShared);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Location updated for provider {ProviderId}", id);

        var response = new LocationResponse
        {
            AddressLine1 = result.AddressLine1,
            Street = result.AddressLine1,
            City = result.City,
            State = result.State,
            Country  =result.Country,
            PostalCode = result.PostalCode,
            Latitude = result.Latitude,
            Longitude = result.Longitude
        };

        return Ok(response);
    }

    #endregion

    #region Working Hours

    /// <summary>
    /// Get provider working hours
    /// </summary>
    [HttpGet("{id:guid}/working-hours")]
    [ProducesResponseType(typeof(WorkingHoursResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWorkingHours(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
     
        var query = new GetProviderByIdQuery(id, false, false);
        var provider = await _mediator.Send(query, cancellationToken);

        if (provider == null)
        {
            return NotFound();
        }

        var response = new WorkingHoursResponse
        {
            BusinessHours = provider.BusinessHours?.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value != null ? new BusinessHoursDetailResponse
                {
                    DayOfWeek = (int)kvp.Key,
                    IsOpen = true,
                    OpenTime = kvp.Value.OpenTime?.ToString("HH:mm") ?? "",
                    CloseTime = kvp.Value.CloseTime?.ToString("HH:mm") ?? ""
                } : null) ?? [],
            
        };

        return Ok(response);
    }

    /// <summary>
    /// Update provider working hours
    /// </summary>
    [HttpPut("{id:guid}/working-hours")]
    [ProducesResponseType(typeof(WorkingHoursResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateWorkingHours(
        [FromRoute] Guid id,
        [FromBody] UpdateWorkingHoursRequest request,
        CancellationToken cancellationToken = default)
    {
 
        // Map request to command DTOs
        var businessHours = new Dictionary<string, DayHoursDto?>();
        foreach (var (dayOfWeek, hours) in request.BusinessHours)
        {
            if (hours == null || !hours.IsOpen)
            {
                businessHours[dayOfWeek] = null;
                continue;
            }

            businessHours[dayOfWeek] = new DayHoursDto(
                hours.DayOfWeek,
                hours.IsOpen,
                hours.OpenTime != null ? new TimeSlotDto(hours.OpenTime.Hours, hours.OpenTime.Minutes) : null,
                hours.CloseTime != null ? new TimeSlotDto(hours.CloseTime.Hours, hours.CloseTime.Minutes) : null,
                hours.Breaks.Select(b => new BreakTimeDto(
                    new TimeSlotDto(b.Start.Hours, b.Start.Minutes),
                    new TimeSlotDto(b.End.Hours, b.End.Minutes))).ToList());
        }

        var command = new UpdateWorkingHoursCommand(id, businessHours);
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Working hours updated for provider {ProviderId}. Working days: {WorkingDaysCount}",
            id, result.WorkingDaysCount);

        return Ok(new UpdateWorkingHours
        { WorkingDaysCount = result.WorkingDaysCount, UpdatedAt = result.UpdatedAt,
            BusinessHours = request.BusinessHours });
    }

    #endregion

    #region Business Hours (New API with Breaks Support)

    /// <summary>
    /// Get provider business hours with breaks
    /// </summary>
    [HttpGet("{id:guid}/business-hours")]
    [Authorize]
    [ProducesResponseType(typeof(Application.Queries.Provider.GetBusinessHours.BusinessHoursViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBusinessHours(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
     

        var query = new GetBusinessHoursQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Update provider business hours with breaks
    /// </summary>
    [HttpPut("{id:guid}/business-hours")]
    [Authorize]
    [ProducesResponseType(typeof(UpdateBusinessHoursResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBusinessHours(
        [FromRoute] Guid id,
        [FromBody] UpdateBusinessHoursRequestDto request,
        CancellationToken cancellationToken = default)
    {
       

        var command = new UpdateBusinessHoursCommand(id, request.BusinessHours);
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Business hours updated for provider {ProviderId}", id);

        return Ok(result);
    }

    #endregion

    #region Holidays

    /// <summary>
    /// Get all holidays for provider
    /// </summary>
    [HttpGet("{id:guid}/holidays")]
    [Authorize]
    [ProducesResponseType(typeof(HolidaysViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHolidays(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
      

        var query = new GetHolidaysQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Add a holiday
    /// </summary>
    [HttpPost("{id:guid}/holidays")]
    [Authorize]
    [ProducesResponseType(typeof(AddHolidayResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddHoliday(
        [FromRoute] Guid id,
        [FromBody] AddHolidayRequestDto request,
        CancellationToken cancellationToken = default)
    {
     

        var command = new AddHolidayCommand(
            id,
            request.Date,
            request.Reason,
            request.IsRecurring,
            request.Pattern);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Holiday added for provider {ProviderId}: {Date} - {Reason}",
            id, request.Date, request.Reason);

        return CreatedAtAction(nameof(GetHolidays), new { id }, result);
    }

    /// <summary>
    /// Delete a holiday
    /// </summary>
    [HttpDelete("{id:guid}/holidays/{holidayId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteHoliday(
        [FromRoute] Guid id,
        [FromRoute] Guid holidayId,
        CancellationToken cancellationToken = default)
    {
       

        var command = new DeleteHolidayCommand(id, holidayId);
        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Holiday {HolidayId} deleted from provider {ProviderId}", holidayId, id);

        return NoContent();
    }

    #endregion

    #region Exceptions

    /// <summary>
    /// Get all exception schedules for provider
    /// </summary>
    [HttpGet("{id:guid}/exceptions")]
    [Authorize]
    [ProducesResponseType(typeof(ExceptionsViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetExceptions(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
      

        var query = new GetExceptionsQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Add an exception schedule
    /// </summary>
    [HttpPost("{id:guid}/exceptions")]
    [Authorize]
    [ProducesResponseType(typeof(AddExceptionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddException(
        [FromRoute] Guid id,
        [FromBody] AddExceptionRequestDto request,
        CancellationToken cancellationToken = default)
    {
     

        var command = new AddExceptionCommand(
            id,
            request.Date,
            request.OpenTime,
            request.CloseTime,
            request.Reason);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Exception added for provider {ProviderId}: {Date} - {Reason}",
            id, request.Date, request.Reason);

        return CreatedAtAction(nameof(GetExceptions), new { id }, result);
    }

    /// <summary>
    /// Delete an exception schedule
    /// </summary>
    [HttpDelete("{id:guid}/exceptions/{exceptionId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteException(
        [FromRoute] Guid id,
        [FromRoute] Guid exceptionId,
        CancellationToken cancellationToken = default)
    {
       

        var command = new DeleteExceptionCommand(id, exceptionId);
        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Exception {ExceptionId} deleted from provider {ProviderId}", exceptionId, id);

        return NoContent();
    }

    #endregion

    #region Availability

    /// <summary>
    /// Get provider availability for a specific date
    /// </summary>
    [HttpGet("{id:guid}/availability")]
    [ProducesResponseType(typeof(AvailabilityViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAvailability(
        [FromRoute] Guid id,
        [FromQuery] string date,
        CancellationToken cancellationToken = default)
    {
       

        if (!DateOnly.TryParse(date, out var dateOnly))
        {
            return BadRequest(new ApiErrorResult("Invalid date format. Use yyyy-MM-dd format.", "INVALID_DATE_FORMAT")
            {
                Errors = new Dictionary<string, string[]>
                {
                    ["date"] = new[] { "Date must be in yyyy-MM-dd format" }
                }
            });
        }

        var query = new GetAvailabilityQuery(id, dateOnly);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    #endregion

    #region Services

    /// <summary>
    /// Get all services for a provider
    /// </summary>
    [HttpGet("{id:guid}/services")]
    [ProducesResponseType(typeof(List<ServiceDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetServices(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
       

        var providerId = ServiceCatalog.Domain.ValueObjects.ProviderId.From(id);
        var services = await _serviceReadRepository.GetByProviderIdAsync(providerId, cancellationToken);

        var response = services.Select(s => new ServiceDetailResponse
        {
            Id = s.Id.Value,
            Name = s.Name,
            Description = s.Description,
            DurationMinutes = s.Duration.Value,
            Price = s.BasePrice.Amount,
            Currency = s.BasePrice.Currency,
            Category = s.Category.ToString(),
            Type = s.Type.ToString(),
            Status = s.Status.ToString()
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Add a new service to provider
    /// </summary>
    [HttpPost("{id:guid}/services")]
    [ProducesResponseType(typeof(ServiceDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddService(
        [FromRoute] Guid id,
        [FromBody] AddServiceRequest request,
        CancellationToken cancellationToken = default)
    {
     

        var command = new AddProviderServiceCommand(
            id,
            request.ServiceName,
            request.Description,
            request.DurationHours,
            request.Duration,
            request.BasePrice,
            request.Currency ?? "IRR",
            request.Category,
            request.IsMobileService);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Service {ServiceId} added to provider {ProviderId}", result.ServiceId, id);

        var response = new ServiceDetailResponse
        {
            Id = result.ServiceId,
            Name = result.ServiceName,
            DurationMinutes = result.TotalDurationMinutes,
            Price = result.Price,
            Currency = result.Currency
        };

        return CreatedAtAction(nameof(GetServices), new { id }, response);
    }

    /// <summary>
    /// Update an existing service
    /// </summary>
    [HttpPut("{id:guid}/services/{serviceId:guid}")]
    [ProducesResponseType(typeof(ServiceDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateService(
        [FromRoute] Guid id,
        [FromRoute] Guid serviceId,
        [FromBody] UpdateProviderServiceRequest request,
        CancellationToken cancellationToken = default)
    {
       

        var command = new UpdateProviderServiceCommand(
            serviceId,
            id,
            request.ServiceName,
            request.Description,
            request.DurationHours,
            request.DurationMinutes,
            request.Price,
            request.Currency ?? "IRR",
            request.Category,
            request.IsMobileService);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Service {ServiceId} updated for provider {ProviderId}", serviceId, id);

        var response = new ServiceDetailResponse
        {
            Id = result.ServiceId,
            Name = result.ServiceName,
            DurationMinutes = result.TotalDurationMinutes,
            Price = result.Price
        };

        return Ok(response);
    }

    /// <summary>
    /// Delete a service
    /// </summary>
    [HttpDelete("{id:guid}/services/{serviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteService(
        [FromRoute] Guid id,
        [FromRoute] Guid serviceId,
        CancellationToken cancellationToken = default)
    {
       
        var command = new DeleteProviderServiceCommand(serviceId, id);
        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Service {ServiceId} deleted from provider {ProviderId}", serviceId, id);

        return NoContent();
    }

    #endregion

    #region Private Helper Methods

    private string? GetCurrentUserId()
    {
        return User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
    }

    private string? GetCurrentUserProviderId()
    {
        return User.FindFirst("providerId")?.Value;
    }


    #endregion
}

#region Response Models

public sealed class BusinessInfoResponse
{
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Website { get; set; }
}

public sealed class LocationResponse
{
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "Iran";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Street { get; set; }
}

public sealed class WorkingHoursResponse
{
    public Dictionary<string, BusinessHoursDetailResponse?> BusinessHours { get; set; } = new();
    //public Dictionary<string, DayScheduleRequest> WorkingHours { get; set; }
}

public sealed class BusinessHoursDetailResponse
{
    public int DayOfWeek { get; set; }
    public bool IsOpen { get; set; }
    public string OpenTime { get; set; } = string.Empty;
    public string CloseTime { get; set; } = string.Empty;
}

public sealed class ServiceDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "IRR";
    public string? Category { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
}

#endregion

#region Request Models for Provider Settings

public sealed class AddServiceRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationHours { get; set; }
    public int Duration { get; set; }
    public decimal BasePrice { get; set; }
    public string? Currency { get; set; }
    public string? Category { get; set; }
    public bool IsMobileService { get; set; }
}

public sealed class UpdateProviderServiceRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationHours { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public string? Category { get; set; }
    public bool IsMobileService { get; set; }
}

/// <summary>
/// Request to update business hours with breaks
/// </summary>
public sealed class UpdateBusinessHoursRequestDto
{
    public List<BusinessHoursDayDto> BusinessHours { get; set; } = new();
}

/// <summary>
/// Request to add a holiday
/// </summary>
public sealed class AddHolidayRequestDto
{
    public DateOnly Date { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public string? Pattern { get; set; }
}

/// <summary>
/// Request to add an exception schedule
/// </summary>
public sealed class AddExceptionRequestDto
{
    public DateOnly Date { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public string Reason { get; set; } = string.Empty;
}

#endregion
