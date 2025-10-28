using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booksy.ServiceCatalog.Application.Commands.Service.ActivateService;
using Booksy.ServiceCatalog.Application.Commands.Service.DeactivateService;
using Booksy.ServiceCatalog.Application.Commands.Service.ArchiveService;
using Booksy.ServiceCatalog.Application.Queries.Service.GetServiceById;
using Booksy.API.Extensions;
using static Booksy.API.Middleware.ExceptionHandlingMiddleware;
using Booksy.ServiceCatalog.Application.Queries.Service.GetServicesByStatus;
using Booksy.ServiceCatalog.Application.Queries.Service.GetPopularServices;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Api.Models.Requests.Extenstions;
using Booksy.ServiceCatalog.Application.Commands.Service.DeleteProviderService;
using Booksy.ServiceCatalog.Application.Commands.Service.UpdateProviderService;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Application.Commands.Service.AddProviderService;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Manages service catalog and service offerings
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status500InternalServerError)]
public class ServicesController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<ServicesController> _logger;
    private readonly IServiceReadRepository _serviceReadRepository;

    public ServicesController(ISender mediator, ILogger<ServicesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


  

    /// <summary>
    /// Update an existing service
    /// </summary>
    [HttpPut("{providerId:guid}/{serviceId:guid}")]
    [ProducesResponseType(typeof(ServiceDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateService(
        [FromRoute] Guid providerId,
        [FromRoute] Guid serviceId,
        [FromBody] UpdateProviderServiceRequest request,
        CancellationToken cancellationToken = default)
    {
      

        var command = new UpdateProviderServiceCommand(
            serviceId,
            providerId,
            request.ServiceName,
            request.Description,
            request.DurationHours,
            request.DurationMinutes,
            request.Price,
            request.Currency ?? "IRR",
            request.Category,
            request.IsMobileService);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Service {ServiceId} updated for provider {ProviderId}", serviceId, providerId);

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
    [HttpDelete("{providerId:guid}/{serviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteService(
        [FromRoute] Guid providerId,
        [FromRoute] Guid serviceId,
        CancellationToken cancellationToken = default)
    {

        var command = new DeleteProviderServiceCommand(serviceId, providerId);
        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Service {ServiceId} deleted from provider {ProviderId}", serviceId, providerId);

        return NoContent();
    }
    /// <summary>
    /// Creates a new service offering
    /// </summary>
    /// <param name="request">Service creation details</param>
    /// <returns>Created service information</returns>
    /// <response code="201">Service successfully created</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">Service with same name already exists for provider</response>
    /// <response code="403">Not authorized to create services for this provider</response>
    [HttpPost("{id:guid}")]
    [Authorize(Policy = "ProviderOrAdmin")]
    [Booksy.API.Middleware.EnableRateLimiting("service-creation")]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status409Conflict)]
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
            request.Category);

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

        return CreatedAtAction(nameof(SearchServices), new { id }, response);
    }
    /// <summary>
    /// Gets a service by its ID
    /// </summary>
    /// <param name="id">Service ID</param>
    /// <param name="includeProvider">Include provider information</param>
    /// <param name="includeOptions">Include service options</param>
    /// <param name="includePriceTiers">Include price tiers</param>
    /// <returns>Service details</returns>
    /// <response code="200">Service found</response>
    /// <response code="404">Service not found</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceById(
        [FromRoute] Guid id,
        [FromQuery] bool includeProvider = false,
        [FromQuery] bool includeOptions = false,
        [FromQuery] bool includePriceTiers = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetServiceByIdQuery(
            ServiceId: id,
            IncludeProvider: includeProvider,
            IncludeOptions: includeOptions,
            IncludePriceTiers: includePriceTiers);

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(MapToServiceDetailsResponse(result));
    }

    /// <summary>
    /// Search services with advanced filtering and pagination
    /// </summary>
    /// <param name="request">Search parameters including pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results</returns>
    /// <response code="200">Services found successfully</response>
    /// <response code="400">Invalid search parameters</response>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ServiceSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ServiceSearchResponse>>> SearchServices(
        [FromQuery] SearchServicesRequest request,
        CancellationToken cancellationToken = default)
    {
        // Convert API request to application query (single line!)
        var query = request.ToQuery();

        // Execute query through MediatR
        var result = await _mediator.Send(query, cancellationToken);

        // Transform to API response model
        var response = result.Map(searchResult => new ServiceSearchResponse
        {
            Id = searchResult.Id,
            ProviderId = searchResult.ProviderId,
            Name = searchResult.Name,
            Description = searchResult.Description,
            Category = searchResult.Category,
            Type = searchResult.Type,
            BasePrice = searchResult.BasePrice,
            Currency = searchResult.Currency,
            Duration = searchResult.Duration,
            Status = searchResult.Status,
            RequiresDeposit = searchResult.RequiresDeposit,
            AvailableAsMobile = searchResult.AvailableAsMobile,
            ImageUrl = searchResult.ImageUrl,
            Provider = new ProviderInfoResponse
            {
                Id = searchResult.Provider.Id,
                BusinessName = searchResult.Provider.BusinessName,
                City = searchResult.Provider.City,
                State = searchResult.Provider.State,
                AllowOnlineBooking = searchResult.Provider.AllowOnlineBooking,
                OffersMobileServices = searchResult.Provider.OffersMobileServices
            }
        });

        // Return with proper pagination headers
        return this.PaginatedOk(response);
    }

    /// <summary>
    /// Gets services for a specific provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="request">Filter parameters including pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of provider's services</returns>
    /// <response code="200">Services retrieved successfully</response>
    /// <response code="400">Invalid filter parameters</response>
    /// <response code="404">Provider not found</response>
    [HttpGet("provider/{providerId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ServiceSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ServiceSummaryResponse>>> GetServicesByProvider(
        [FromRoute] Guid providerId,
        [FromQuery] GetServicesByProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        // Convert API request to application query
        var query = request.ToQuery(providerId);

        // Execute query through MediatR
        var result = await _mediator.Send(query, cancellationToken);

        // Transform to API response model
        var response = result.Map(serviceResult => new ServiceSummaryResponse
        {
            Id = serviceResult.Id,
            ProviderId = serviceResult.ProviderId,
            Name = serviceResult.Name,
            Description = serviceResult.Description,
            Category = serviceResult.Category,
            Type = serviceResult.Type,
            BasePrice = serviceResult.BasePrice,
            Currency = serviceResult.Currency,
            Duration = serviceResult.Duration,
            Status = serviceResult.Status,
            RequiresDeposit = serviceResult.RequiresDeposit,
            AvailableAsMobile = serviceResult.AvailableAsMobile,
            ImageUrl = serviceResult.ImageUrl
        });

        return this.PaginatedOk(response);
    }

    

    /// <summary>
    /// Activates a service
    /// </summary>
    /// <param name="id">Service ID</param>
    /// <returns>Success message</returns>
    /// <response code="200">Service activated successfully</response>
    /// <response code="400">Service cannot be activated</response>
    /// <response code="404">Service not found</response>
    /// <response code="403">Not authorized to activate this service</response>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "ProviderOrAdmin")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivateService(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageService(id))
        {
            _logger.LogWarning("User {RequestingUser} attempted to activate service {ServiceId} without permission",
                GetCurrentUserId(), id);
            return Forbid();
        }

        var command = new ActivateServiceCommand(ServiceId: id);
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Service {ServiceId} activated successfully by user {UserId}", id, GetCurrentUserId());
        return Ok(new MessageResponse("Service activated successfully"));
    }

    /// <summary>
    /// Deactivates a service
    /// </summary>
    /// <param name="id">Service ID</param>
    /// <param name="request">Deactivation reason</param>
    /// <returns>Success message</returns>
    /// <response code="200">Service deactivated successfully</response>
    /// <response code="404">Service not found</response>
    /// <response code="403">Not authorized to deactivate this service</response>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "ProviderOrAdmin")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateService(
        [FromRoute] Guid id,
        [FromBody] DeactivateServiceRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageService(id))
        {
            _logger.LogWarning("User {RequestingUser} attempted to deactivate service {ServiceId} without permission",
                GetCurrentUserId(), id);
            return Forbid();
        }

        var command = new DeactivateServiceCommand(
            ServiceId: id,
            Reason: request?.Reason ?? "Deactivated by user");
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Service {ServiceId} deactivated by user {UserId}", id, GetCurrentUserId());
        return Ok(new MessageResponse("Service deactivated successfully"));
    }

    /// <summary>
    /// Archives a service (soft delete)
    /// </summary>
    /// <param name="id">Service ID</param>
    /// <param name="request">Archive reason</param>
    /// <returns>Success message</returns>
    /// <response code="204">Service archived successfully</response>
    /// <response code="404">Service not found</response>
    /// <response code="403">Not authorized to archive this service</response>
    /// <response code="409">Cannot archive service with active bookings</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ProviderOrAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ArchiveService(
        [FromRoute] Guid id,
        [FromBody] ArchiveServiceRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageService(id))
        {
            _logger.LogWarning("User {RequestingUser} attempted to archive service {ServiceId} without permission",
                GetCurrentUserId(), id);
            return Forbid();
        }

        var command = new ArchiveServiceCommand(
            ServiceId: id,
            Reason: request?.Reason ?? "Archived by user");
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogWarning("Service {ServiceId} archived by user {UserId}", id, GetCurrentUserId());
        return NoContent();
    }

    /// <summary>
    /// Gets services filtered by status
    /// </summary>
    /// <param name="status">Service status to filter by</param>
    /// <param name="maxResults">Maximum number of results (default: 100, max: 1000)</param>
    /// <returns>List of services with the specified status</returns>
    /// <response code="200">Services retrieved successfully</response>
    /// <response code="400">Invalid status provided</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (admin only)</response>
    [HttpGet("by-status/{status}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetServicesByStatus(
        [FromRoute] ServiceStatus status,
        [FromQuery] int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        var query = new GetServicesByStatusQuery(status, maxResults);
        var result = await _mediator.Send(query, cancellationToken);

        var response = result.Select(MapToServiceSummaryResponse).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Gets popular services across all providers
    /// </summary>
    /// <param name="categoryFilter">Optional category filter</param>
    /// <param name="limit">Number of services to return (default: 20, max: 100)</param>
    /// <returns>List of popular services</returns>
    /// <response code="200">Popular services retrieved successfully</response>
    /// <response code="400">Invalid parameters</response>
    [HttpGet("popular")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPopularServices(
        [FromQuery] string? categoryFilter = null,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0 || limit > 100)
            return BadRequest("Limit must be between 1 and 100");

        var query = new GetPopularServicesQuery(categoryFilter, limit);
        var result = await _mediator.Send(query, cancellationToken);

        var response = result.Select(MapToServiceSummaryResponse).ToList();
        return Ok(response);
    }

    #region Private Helper Methods


 


  

    private ServiceDetailsResponse MapToServiceDetailsResponse(ServiceDetailsViewModel result)
    {
        return new ServiceDetailsResponse
        {
            Id = result.Id,
            ProviderId = result.ProviderId,
            Name = result.Name,
            Description = result.Description,
            Category = result.Category,
            Type = result.Type.ToString(),
            BasePrice = result.BasePrice,
            Currency = result.Currency,
            Duration = result.Duration,
            PreparationTime = result.PreparationTime,
            BufferTime = result.BufferTime,
            TotalDuration = result.TotalDuration,
            Status = result.Status.ToString(),
            RequiresDeposit = result.RequiresDeposit,
            DepositPercentage = result.DepositPercentage,
            DepositAmount = result.DepositAmount,
            AllowOnlineBooking = result.AllowOnlineBooking,
            AvailableAtLocation = result.AvailableAtLocation,
            AvailableAsMobile = result.AvailableAsMobile,
            MaxAdvanceBookingDays = result.MaxAdvanceBookingDays,
            MinAdvanceBookingHours = result.MinAdvanceBookingHours,
            MaxConcurrentBookings = result.MaxConcurrentBookings,
            ImageUrl = result.ImageUrl,
            QualifiedStaffCount = result.QualifiedStaffCount,
            CanBeBooked = result.CanBeBooked,
            CreatedAt = result.CreatedAt,
            ActivatedAt = result.ActivatedAt,
            Tags = result.Tags.ToList(),
            Options = result.Options.Select(o => new ServiceOptionResponse
            {
                Id = o.Id,
                Name = o.Name,
                Description = o.Description,
                AdditionalPrice = o.AdditionalPrice,
                Currency = o.Currency,
                AdditionalDuration = o.AdditionalDuration,
                IsRequired = o.IsRequired,
                IsActive = o.IsActive,
                SortOrder = o.SortOrder
            }).ToList(),
            PriceTiers = result.PriceTiers.Select(pt => new PriceTierResponse
            {
                Id = pt.Id,
                Name = pt.Name,
                Description = pt.Description,
                Price = pt.Price,
                Currency = pt.Currency,
                IsDefault = pt.IsDefault,
                IsActive = pt.IsActive,
                SortOrder = pt.SortOrder
            }).ToList(),
            Provider = result.Provider != null ? new ProviderSummaryResponse
            {
                Id = result.Provider.Id,
                BusinessName = result.Provider.BusinessName,
                Description = result.Provider.Description,
                Type = result.Provider.Type.ToString(),
                City = result.Provider.City,
                State = result.Provider.State,
                LogoUrl = result.Provider.LogoUrl,
                AllowOnlineBooking = result.Provider.AllowOnlineBooking,
                OffersMobileServices = result.Provider.OffersMobileServices
            } : null
        };
    }

    private ServiceSummaryResponse MapToServiceSummaryResponse(dynamic service)
    {
        return new ServiceSummaryResponse
        {
            Id = service.Id,
            ProviderId = service.ProviderId,
            Name = service.Name,
            Description = service.Description,
            Category = service.Category,
            Type = service.Type.ToString(),
            BasePrice = service.BasePrice,
            Currency = service.Currency,
            Duration = service.Duration,
            Status = service.Status.ToString(),
            RequiresDeposit = service.RequiresDeposit,
            AvailableAsMobile = service.AvailableAsMobile,
            ImageUrl = service.ImageUrl,
            Tags = service.Tags?.ToList() ?? new List<string>()
        };
    }

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

        // Additional business logic could go here (e.g., staff with management permissions)
        return false;
    }

    private async Task<bool> CanManageService(Guid serviceId)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return false;

        // Admins can manage any service
        if (User.IsInRole("Admin") || User.IsInRole("SysAdmin"))
            return true;

        // For now, we'd need to query the service to get its provider
        // This would typically involve a lightweight query to get just the provider ID
        // For demonstration, we'll assume this is implemented
        // var service = await GetServiceProviderInfo(serviceId);
        // return await CanManageProvider(service.ProviderId);

        // Simplified check - providers can manage services in their context
        return User.IsInRole("Provider");
    }

    #endregion
}