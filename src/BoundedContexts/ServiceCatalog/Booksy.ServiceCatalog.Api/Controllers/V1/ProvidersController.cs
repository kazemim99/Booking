using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider;
using Booksy.ServiceCatalog.Application.Commands.Provider.ActivateProvider;
using Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProviderFull;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByStatus;
using Booksy.API.Extensions;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Api.Models.Requests.Extenstions;
using Booksy.API.Middleware;
using static Booksy.API.Middleware.ExceptionHandlingMiddleware;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.API.Controllers.V1;

/// <summary>
/// Manages service providers and their business profiles
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status500InternalServerError)]
public class ProvidersController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<ProvidersController> _logger;

    public ProvidersController(ISender mediator, ILogger<ProvidersController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new service provider (simple registration)
    /// </summary>
    /// <param name="request">Provider registration details</param>
    /// <returns>Created provider information</returns>
    /// <response code="201">Provider successfully created</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">Provider already exists</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [Booksy.API.Middleware.EnableRateLimiting("provider-registration")]
    [ProducesResponseType(typeof(ProviderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterProvider(
        [FromBody] RegisterProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = MapToRegisterCommand(request);
        var result = await _mediator.Send(command, cancellationToken);

        var response = MapToProviderResponse(result);

        _logger.LogInformation("Provider {ProviderId} registered successfully", response.Id);

        return CreatedAtAction(
            nameof(GetProviderById),
            new { id = response.Id, version = "1.0" },
            response);
    }

    /// <summary>
    /// Registers a new service provider with complete multi-step data
    /// </summary>
    /// <param name="request">Complete provider registration data from multi-step form</param>
    /// <returns>Created provider information with all details</returns>
    /// <response code="201">Provider successfully created with all data</response>
    /// <response code="400">Invalid request data or validation failure</response>
    /// <response code="409">Provider already exists for this user</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("register-full")]
    [Authorize]
    [Booksy.API.Middleware.EnableRateLimiting("provider-registration")]
    [ProducesResponseType(typeof(ProviderFullRegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterProviderFull(
        [FromBody] RegisterProviderFullRequest request,
        CancellationToken cancellationToken = default)
    {
        // Ensure the OwnerId matches the authenticated user
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized("User not authenticated");
        }

        if (request.OwnerId.ToString() != currentUserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to register provider for different owner {OwnerId}",
                currentUserId,
                request.OwnerId);
            return Forbid();
        }

        var command = MapToRegisterFullCommand(request);
        var result = await _mediator.Send(command, cancellationToken);

        var response = new ProviderFullRegistrationResponse
        {
            ProviderId = result.ProviderId,
            BusinessName = result.BusinessName,
            Status = result.Status.ToString(),
            RegisteredAt = result.RegisteredAt,
            ServicesCount = result.ServicesCount,
            StaffCount = result.StaffCount,
            Message = "Provider registration submitted successfully. Pending admin approval."
        };

        _logger.LogInformation(
            "Provider {ProviderId} fully registered by user {UserId} with {ServiceCount} services and {StaffCount} staff",
            response.ProviderId,
            currentUserId,
            result.ServicesCount,
            result.StaffCount);

        return CreatedAtAction(
            nameof(GetProviderById),
            new { id = response.ProviderId, version = "1.0" },
            response);
    }

    /// <summary>
    /// Gets a provider by their ID
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <param name="includeServices">Include provider's services</param>
    /// <param name="includeStaff">Include provider's staff</param>
    /// <returns>Provider details</returns>
    /// <response code="200">Provider found</response>
    /// <response code="404">Provider not found</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProviderDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProviderById(
        [FromRoute] Guid id,
        [FromQuery] bool includeServices = false,
        [FromQuery] bool includeStaff = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProviderByIdQuery(
            ProviderId: id,
            IncludeServices: includeServices,
            IncludeStaff: includeStaff);

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(MapToProviderDetailsResponse(result));
    }

    /// <summary>
    /// Search providers with advanced filtering and pagination
    /// </summary>
    /// <param name="request">Search parameters including pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results</returns>
    /// <response code="200">Providers found successfully</response>
    /// <response code="400">Invalid search parameters</response>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ProviderSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProviderSearchResponse>>> SearchProviders(
        [FromQuery] SearchProvidersRequest request,
        CancellationToken cancellationToken = default)
    {
        // Convert API request to application query (single line!)
        var query = request.ToQuery();

        // Execute query through MediatR
        var result = await _mediator.Send(query, cancellationToken);

        // Transform to API response model
        var response = result.Map(searchResult => new ProviderSearchResponse
        {
            Id = searchResult.Id,
            BusinessName = searchResult.BusinessName,
            Description = searchResult.Description,
            Type = searchResult.Type.ToString(),
            Status = searchResult.Status.ToString(),
            City = searchResult.City,
            State = searchResult.State,
            Country = searchResult.Country,
            LogoUrl = searchResult.LogoUrl,
            AllowOnlineBooking = searchResult.AllowOnlineBooking,
            OffersMobileServices = searchResult.OffersMobileServices,
            AverageRating = searchResult.AverageRating,
            ServiceCount = searchResult.ServiceCount,
            YearsInBusiness = searchResult.YearsInBusiness,
            IsVerified = searchResult.IsVerified,
            RegisteredAt = searchResult.RegisteredAt,
            LastActiveAt = searchResult.LastActiveAt
        });

        // Return with proper pagination headers
        return this.PaginatedOk(response);
    }

    /// <summary>
    /// Gets providers by location within specified radius
    /// </summary>
    /// <param name="request">Location search parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of nearby providers</returns>
    /// <response code="200">Providers retrieved successfully</response>
    /// <response code="400">Invalid location parameters</response>
    [HttpGet("by-location")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ProviderLocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProviderLocationResponse>>> GetProvidersByLocation(
        [FromQuery] GetProvidersByLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Convert API request to application query
        var query = request.ToQuery();

        // Execute query through MediatR
        var result = await _mediator.Send(query, cancellationToken);

        // Transform to API response model
        var response = result.Map(locationResult => new ProviderLocationResponse
        {
            Id = locationResult.Id,
            BusinessName = locationResult.BusinessName,
            Description = locationResult.Description,
            Type = locationResult.Type.ToString(),
            Status = locationResult.Status.ToString(),
            Address = new AddressResponse
            {
                Street = locationResult.Address.Street,
                City = locationResult.Address.City,
                State = locationResult.Address.State,
                PostalCode = locationResult.Address.PostalCode,
                Country = locationResult.Address.Country
            },
            Coordinates = new CoordinatesResponse
            {
                Latitude = locationResult.Coordinates.Latitude,
                Longitude = locationResult.Coordinates.Longitude
            },
            DistanceKm = locationResult.DistanceKm,
            LogoUrl = locationResult.LogoUrl,
            AllowOnlineBooking = locationResult.AllowOnlineBooking,
            OffersMobileServices = locationResult.OffersMobileServices,
            AverageRating = locationResult.AverageRating,
            ServiceCount = locationResult.ServiceCount
        });

        return this.PaginatedOk(response);
    }

    /// <summary>
    /// Updates a provider's business profile
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <param name="request">Updated business profile information</param>
    /// <returns>Updated provider information</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid update data</response>
    /// <response code="404">Provider not found</response>
    /// <response code="403">Not authorized to update this profile</response>
    //[HttpPut("{id:guid}/profile")]
    //[Authorize]
    //[ProducesResponseType(typeof(ProviderDetailsResponse), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //public async Task<IActionResult> UpdateBusinessProfile(
    //    [FromRoute] Guid id,
    //    [FromBody][Required] UpdateBusinessProfileRequest request,
    //    CancellationToken cancellationToken = default)
    //{
    //    // Providers can only update their own profile unless they're an admin
    //    if (!await CanManageProvider(id))
    //    {
    //        _logger.LogWarning("User {RequestingUser} attempted to update provider {ProviderId} without permission",
    //            GetCurrentUserId(), id);
    //        return Forbid();
    //    }

    //    var command = new UpdateBusinessProfileCommand(
    //        ProviderId: id,
    //        BusinessName: request.BusinessName,
    //        Description: request.Description,
    //        LogoUrl: request.LogoUrl,
    //        Website: request.WebsiteUrl);

    //    var result = await _mediator.Send(command, cancellationToken);

    //    _logger.LogInformation("Provider {ProviderId} profile updated successfully", id);
    //    return Ok(MapToProviderDetailsResponse(result));
    //}

    /// <summary>
    /// Activates a provider account
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <param name="request">Activation details</param>
    /// <returns>Success message</returns>
    /// <response code="200">Account activated successfully</response>
    /// <response code="400">Invalid activation or provider already active</response>
    /// <response code="404">Provider not found</response>
    /// <response code="403">Not authorized (admin only)</response>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivateProvider(
        [FromRoute] Guid id,
        [FromBody] ActivateProviderRequest? request,
        CancellationToken cancellationToken = default)
    {
        var command = new ActivateProviderCommand(
            ProviderId: id);
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Provider {ProviderId} activated by admin {AdminId}", id, GetCurrentUserId());
        return Ok(new MessageResponse("Provider account activated successfully"));
    }

    ///// <summary>
    ///// Deactivates a provider account (Admin only)
    ///// </summary>
    ///// <param name="id">Provider ID</param>
    ///// <param name="request">Deactivation reason</param>
    ///// <returns>Success message</returns>
    ///// <response code="200">Account deactivated successfully</response>
    ///// <response code="404">Provider not found</response>
    ///// <response code="403">Not authorized (admin only)</response>
    //[HttpPost("{id:guid}/deactivate")]
    //[Authorize(Policy = "AdminOnly")]
    //[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //public async Task<IActionResult> DeactivateProvider(
    //    [FromRoute] Guid id,
    //    [FromBody] DeactivateProviderRequest? request,
    //    CancellationToken cancellationToken = default)
    //{
    //    var command = new DeactivateProviderCommand(
    //        ProviderId: id,
    //        Reason: request?.Reason ?? "Deactivated by administrator",
    //        Notes: request?.Notes);
    //    var result = await _mediator.Send(command, cancellationToken);

    //    _logger.LogInformation("Provider {ProviderId} deactivated by admin {AdminId}", id, GetCurrentUserId());
    //    return Ok(new MessageResponse("Provider account deactivated successfully"));
    //}

    /// <summary>
    /// Permanently archives a provider account (Admin only)
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <param name="request">Archive reason</param>
    /// <returns>Success message</returns>
    /// <response code="204">Account archived successfully</response>
    /// <response code="404">Provider not found</response>
    /// <response code="403">Not authorized (admin only)</response>
    /// <response code="409">Cannot archive provider with active bookings</response>
    //[HttpDelete("{id:guid}")]
    //[Authorize(Policy = "SysAdminOnly")]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status409Conflict)]
    //public async Task<IActionResult> ArchiveProvider(
    //    [FromRoute] Guid id,
    //    [FromBody] ArchiveProviderRequest? request,
    //    CancellationToken cancellationToken = default)
    //{
    //    var command = new ArchiveProviderCommand(
    //        ProviderId: id,
    //        Reason: request?.Reason ?? "Archived by administrator",
    //        Notes: request?.Notes);
    //    var result = await _mediator.Send(command, cancellationToken);

    //    _logger.LogWarning("Provider {ProviderId} permanently archived by admin {AdminId}", id, GetCurrentUserId());
    //    return NoContent();
    //}

    /// <summary>
    /// Gets providers filtered by status
    /// </summary>
    /// <param name="status">Provider status to filter by</param>
    /// <param name="maxResults">Maximum number of results (default: 100, max: 1000)</param>
    /// <returns>List of providers with the specified status</returns>
    /// <response code="200">Providers retrieved successfully</response>
    /// <response code="400">Invalid status provided</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (admin only)</response>
    [HttpGet("by-status/{status}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<ProviderSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProvidersByStatus(
        [FromRoute] ProviderStatus status,
        [FromQuery] int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProvidersByStatusQuery(status, maxResults);
        var result = await _mediator.Send(query, cancellationToken);

        var response = result.Select(MapToProviderSummaryResponse).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Gets featured providers across all categories
    /// </summary>
    /// <param name="categoryFilter">Optional category filter</param>
    /// <param name="limit">Number of providers to return (default: 20, max: 100)</param>
    /// <returns>List of featured providers</returns>
    /// <response code="200">Featured providers retrieved successfully</response>
    /// <response code="400">Invalid parameters</response>
    //[HttpGet("featured")]
    //[AllowAnonymous]
    //[ProducesResponseType(typeof(IReadOnlyList<ProviderSummaryResponse>), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //public async Task<IActionResult> GetFeaturedProviders(
    //    [FromQuery] string? categoryFilter = null,
    //    [FromQuery] int limit = 20,
    //    CancellationToken cancellationToken = default)
    //{
    //    if (limit <= 0 || limit > 100)
    //        return BadRequest("Limit must be between 1 and 100");

    //    var query = new GetFeaturedProvidersQuery(categoryFilter, limit);
    //    var result = await _mediator.Send(query, cancellationToken);

    //    var response = result.Select(MapToProviderSummaryResponse).ToList();
    //    return Ok(response);
    //}

    /// <summary>
    /// Updates provider verification status (Admin only)
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <param name="request">Verification details</param>
    /// <returns>Success message</returns>
    /// <response code="200">Verification status updated successfully</response>
    /// <response code="404">Provider not found</response>
    /// <response code="403">Not authorized (admin only)</response>
    //[HttpPost("{id:guid}/verify")]
    //[Authorize(Policy = "AdminOnly")]
    //[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //public async Task<IActionResult> UpdateVerificationStatus(
    //    [FromRoute] Guid id,
    //    [FromBody][Required] UpdateVerificationRequest request,
    //    CancellationToken cancellationToken = default)
    //{
    //    var command = new UpdateProviderVerificationCommand(
    //        ProviderId: id,
    //        IsVerified: request.IsVerified,
    //        VerificationNotes: request.VerificationNotes);

    //    var result = await _mediator.Send(command, cancellationToken);

    //    var status = request.IsVerified ? "verified" : "unverified";
    //    _logger.LogInformation("Provider {ProviderId} marked as {Status} by admin {AdminId}",
    //        id, status, GetCurrentUserId());

    //    return Ok(new MessageResponse($"Provider verification status updated to {status}"));
    //}

    #region Private Helper Methods

    private RegisterProviderCommand MapToRegisterCommand(RegisterProviderRequest request)
    {
        return new RegisterProviderCommand(
            OwnerId: request.OwnerId,
            BusinessName: request.BusinessName,
            Description: request.Description,
            ProviderType: request.Type,
            PrimaryPhone: request.ContactInfo.PrimaryPhone,
            SecondaryPhone: request.ContactInfo.SecondaryPhone,Website: request.WebsiteUrl,request.Address.Street,request.Address.City,
            request.Address.State,request.Address.PostalCode,request.Address.Country,request.Address.Longitude,request.Address.Latitude
            );
    }

    private RegisterProviderFullCommand MapToRegisterFullCommand(RegisterProviderFullRequest request)
    {
        // Map business info
        var businessInfo = new BusinessInfoDto(
            request.BusinessInfo.BusinessName,
            request.BusinessInfo.OwnerFirstName,
            request.BusinessInfo.OwnerLastName,
            request.BusinessInfo.PhoneNumber);

        // Map address
        var address = new AddressDto(
            request.Address.Street,
            null, // AddressLine2 is combined in Street
            request.Address.City,
            request.Address.PostalCode);

        // Map location
        LocationDto? location = request.Location != null
            ? new LocationDto(
                request.Location.Latitude,
                request.Location.Longitude,
                request.Location.FormattedAddress)
            : null;

        // Map business hours
        var businessHours = new Dictionary<int, DayHoursDto?>();
        foreach (var (dayOfWeek, hours) in request.BusinessHours)
        {
            if (hours == null)
            {
                businessHours[dayOfWeek] = null;
                continue;
            }

            var openTime = hours.OpenTime != null
                ? new TimeSlotDto(hours.OpenTime.Hours, hours.OpenTime.Minutes)
                : null;

            var closeTime = hours.CloseTime != null
                ? new TimeSlotDto(hours.CloseTime.Hours, hours.CloseTime.Minutes)
                : null;

            var breaks = hours.Breaks.Select(b => new BreakTimeDto(
                new TimeSlotDto(b.Start.Hours, b.Start.Minutes),
                new TimeSlotDto(b.End.Hours, b.End.Minutes))).ToList();

            businessHours[dayOfWeek] = new DayHoursDto(
                hours.DayOfWeek,
                hours.IsOpen,
                openTime,
                closeTime,
                breaks);
        }

        // Map services
        var services = request.Services.Select(s => new ServiceDto(
            s.Name,
            s.DurationHours,
            s.DurationMinutes,
            s.Price,
            s.PriceType)).ToList();

        // Map team members
        var teamMembers = request.TeamMembers.Select(m => new TeamMemberDto(
            m.Name,
            m.Email,
            m.PhoneNumber,
            m.CountryCode,
            m.Position,
            m.IsOwner)).ToList();

        return new RegisterProviderFullCommand(
            request.OwnerId,
            request.CategoryId,
            businessInfo,
            address,
            location,
            businessHours,
            services,
            request.AssistanceOptions,
            teamMembers);
    }

    private ProviderResponse MapToProviderResponse(dynamic result)
    {
        return new ProviderResponse
        {
            Id = result.ProviderId,
            BusinessName = result.BusinessName,
            Type = result.Type.ToString(),
            Status = result.Status.ToString(),
            RegisteredAt = result.RegisteredAt
        };
    }

    private ProviderDetailsResponse MapToProviderDetailsResponse(ProviderDetailsViewModel result)
    {
        return new ProviderDetailsResponse
        {
            Id = result.Id,
            OwnerId = result.OwnerId,
            BusinessName = result.BusinessName,
            Description = result.Description,
            Type = result.Type.ToString(),
            Status = result.Status.ToString(),
            ContactInfo = new ContactInfoResponse
            {
                Email = result.ContactInfo.Email,
                PrimaryPhone = result.ContactInfo.Phone,
                Website = result.ContactInfo.Website
            },
            Address = new AddressResponse
            {
                Street = result.Address.Street,
                City = result.Address.City,
                State = result.Address.State,
                PostalCode = result.Address.PostalCode,
                Country = result.Address.Country
            },
            BusinessHours = result.BusinessHours.ToDictionary(
                bh => bh.Key,
                bh => bh.Value != null ? new BusinessHoursResponse
                {
                    OpenTime = bh.Value.OpenTime,
                    CloseTime = bh.Value.CloseTime
                } : null),
            LogoUrl = result.LogoUrl,
            WebsiteUrl = result.WebsiteUrl,
            AllowOnlineBooking = result.AllowOnlineBooking,
            OffersMobileServices = result.OffersMobileServices,
            IsVerified = result.IsVerified,
            AverageRating = result.AverageRating,
            TotalReviews = result.TotalReviews,
            ServiceCount = result.ServiceCount,
            StaffCount = result.StaffCount,
            YearsInBusiness = result.YearsInBusiness,
            Tags = result.Tags.ToList(),
            RegisteredAt = result.RegisteredAt,
            ActivatedAt = result.ActivatedAt,
            LastActiveAt = result.LastActiveAt,
            Services = result.Services?.Select(static s => new ServiceSummaryResponse
            {
                Id = s.Id,
                ProviderId = s.ProviderId,
                Name = s.Name,
                Description = s.Description,
                Category = s.Category,
                Type = s.Type,
                BasePrice = s.BasePrice,
                Currency = s.Currency,
                Duration = s.Duration,
                Status = s.Status.ToString(),
                ImageUrl = s.ImageUrl,
            }).ToList() ?? new List<ServiceSummaryResponse>()
        };
    }

    private ProviderSummaryResponse MapToProviderSummaryResponse(dynamic provider)
    {
        return new ProviderSummaryResponse
        {
            Id = provider.Id,
            BusinessName = provider.BusinessName,
            Description = provider.Description,
            Type = provider.Type.ToString(),
            Status = provider.Status.ToString(),
            City = provider.City,
            State = provider.State,
            Country = provider.Country,
            LogoUrl = provider.LogoUrl,
            AllowOnlineBooking = provider.AllowOnlineBooking,
            OffersMobileServices = provider.OffersMobileServices,
            IsVerified = provider.IsVerified,
            AverageRating = provider.AverageRating,
            TotalReviews = provider.TotalReviews,
            ServiceCount = provider.ServiceCount,
            RegisteredAt = provider.RegisteredAt
        };
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
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

        // Business logic: Staff with management permissions could go here
        return false;
    }

    #endregion
}
