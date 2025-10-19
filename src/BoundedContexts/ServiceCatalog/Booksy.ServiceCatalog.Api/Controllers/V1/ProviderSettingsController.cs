using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Responses;
using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessInfo;
using Booksy.ServiceCatalog.Application.Commands.Provider.UpdateLocation;
using Booksy.ServiceCatalog.Application.Commands.Provider.UpdateWorkingHours;
using Booksy.ServiceCatalog.Application.Commands.Service.AddProviderService;
using Booksy.ServiceCatalog.Application.Commands.Service.UpdateProviderService;
using Booksy.ServiceCatalog.Application.Commands.Service.DeleteProviderService;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById;
using Booksy.ServiceCatalog.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Booksy.API.Middleware.ExceptionHandlingMiddleware;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Provider settings management - allows providers to update their profile sections independently
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/providers")]
[Produces("application/json")]
[Authorize] // All endpoints require authentication
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
        if (!await CanManageProviderAsync(id))
        {
            _logger.LogWarning("User {UserId} attempted to access provider {ProviderId} business info without permission",
                GetCurrentUserId(), id);
            return Forbid();
        }

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
        // Authorization check
        if (!await CanManageProviderAsync(id))
        {
            _logger.LogWarning("User {UserId} attempted to update provider {ProviderId} business info without permission",
                GetCurrentUserId(), id);
            return Forbid();
        }

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
        if (!await CanManageProviderAsync(id))
        {
            return Forbid();
        }

        var query = new GetProviderByIdQuery(id, false, false);
        var provider = await _mediator.Send(query, cancellationToken);

        if (provider == null)
        {
            return NotFound();
        }

        var response = new LocationResponse
        {
            AddressLine1 = provider.Address?.Street ?? "",
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
        if (!await CanManageProviderAsync(id))
        {
            return Forbid();
        }

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
            City = result.City,
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
        if (!await CanManageProviderAsync(id))
        {
            return Forbid();
        }

        var query = new GetProviderByIdQuery(id, false, false);
        var provider = await _mediator.Send(query, cancellationToken);

        if (provider == null)
        {
            return NotFound();
        }

        var response = new WorkingHoursResponse
        {
            BusinessHours = provider.BusinessHours?.ToDictionary(
                kvp => (int)kvp.Key,
                kvp => kvp.Value != null ? new BusinessHoursDetailResponse
                {
                    DayOfWeek = (int)kvp.Key,
                    IsOpen = true,
                    OpenTime = kvp.Value.OpenTime?.ToString("HH:mm") ?? "",
                    CloseTime = kvp.Value.CloseTime?.ToString("HH:mm") ?? ""
                } : null) ?? new Dictionary<int, BusinessHoursDetailResponse?>()
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
        if (!await CanManageProviderAsync(id))
        {
            return Forbid();
        }

        // Map request to command DTOs
        var businessHours = new Dictionary<int, DayHoursDto?>();
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

        return Ok(new { workingDaysCount = result.WorkingDaysCount, updatedAt = result.UpdatedAt });
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
        if (!await CanManageProviderAsync(id))
        {
            return Forbid();
        }

        var providerId = ServiceCatalog.Domain.ValueObjects.ProviderId.Create(id);
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
        if (!await CanManageProviderAsync(id))
        {
            return Forbid();
        }

        var command = new AddProviderServiceCommand(
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
        if (!await CanManageProviderAsync(id))
        {
            return Forbid();
        }

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
        if (!await CanManageProviderAsync(id))
        {
            return Forbid();
        }

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

    private async Task<bool> CanManageProviderAsync(Guid providerId)
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

        // TODO: Check if user is the owner of the provider (query database)
        // For now, only allow if providerId claim matches
        return false;
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
}

public sealed class WorkingHoursResponse
{
    public Dictionary<int, BusinessHoursDetailResponse?> BusinessHours { get; set; } = new();
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
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
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

#endregion
