using Booksy.API.Extensions;
using Booksy.API.Middleware;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Requests.Extenstions;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Responses;
using Booksy.ServiceCatalog.Application.Commands.Provider.ActivateProvider;
using Booksy.ServiceCatalog.Application.Commands.Provider.ActivateProviderStaff;
using Booksy.ServiceCatalog.Application.Commands.Provider.AddStaffToProvider;
using Booksy.ServiceCatalog.Application.Commands.Provider.CompleteProviderRegistration;
using Booksy.ServiceCatalog.Application.Commands.Provider.CreateProviderDraft;
using Booksy.ServiceCatalog.Application.Commands.Provider.DeactivateProviderStaff;
using Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider;
using Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProviderFull;
using Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessProfile;
using Booksy.ServiceCatalog.Application.Commands.Provider.UpdateProviderStaff;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetCurrentProviderStatus;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetDraftProvider;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderById;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderByOwnerId;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProvidersByStatus;
using Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderStaff;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Booksy.API.Middleware.ExceptionHandlingMiddleware;

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
    private readonly Application.Services.IImageStorageService _imageStorageService;

    public ProvidersController(
        ISender mediator,
        ILogger<ProvidersController> logger,
        Application.Services.IImageStorageService imageStorageService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _imageStorageService = imageStorageService ?? throw new ArgumentNullException(nameof(imageStorageService));
    }



    #region Progressive Registration Flow

    /// <summary>
    /// Creates a draft provider (Step 3 of registration flow)
    /// </summary>
    /// <remarks>
    /// Creates a draft provider after user completes business info, category, and location steps.
    /// If user already has a draft provider, returns the existing draft.
    /// </remarks>
    /// <param name="request">Draft provider data from Step 3</param>
    /// <returns>Created or existing draft provider information</returns>
    /// <response code="201">Draft provider created successfully</response>
    /// <response code="200">Existing draft provider returned</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("draft")]
    [Authorize]
    [Booksy.API.Middleware.EnableRateLimiting("provider-registration")]
    [ProducesResponseType(typeof(CreateProviderDraftResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CreateProviderDraftResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProviderDraft(
        [FromBody] CreateProviderDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateProviderDraftCommand(
            BusinessName: request.BusinessName,
            BusinessDescription: request.BusinessDescription,
            Category: request.Category,
            PhoneNumber: request.PhoneNumber,
            Email: request.Email,
            AddressLine1: request.AddressLine1,
            AddressLine2: request.AddressLine2,
            City: request.City,
            Province: request.Province,
            PostalCode: request.PostalCode,
            Latitude: request.Latitude,
            Longitude: request.Longitude
        );

        var result = await _mediator.Send(command, cancellationToken);

        var response = new CreateProviderDraftResponse
        {
            ProviderId = result.ProviderId,
            RegistrationStep = result.RegistrationStep,
            Message = result.Message
        };

        _logger.LogInformation("Draft provider {ProviderId} created/resumed at step {Step}",
            result.ProviderId, result.RegistrationStep);

        // Return 200 if existing draft was found, 201 if new draft created
        if (result.Message.Contains("already exists"))
            return Ok(response);

        return CreatedAtAction(
            nameof(GetProviderById),
            new { id = response.ProviderId, version = "1.0" },
            response);
    }

    /// <summary>
    /// Gets the current user's draft provider
    /// </summary>
    /// <returns>Draft provider data if exists</returns>
    /// <response code="200">Draft provider found</response>
    /// <response code="404">No draft provider found</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("draft")]
    [Authorize]
    [ProducesResponseType(typeof(GetDraftProviderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDraftProvider(
        CancellationToken cancellationToken = default)
    {
        var query = new GetDraftProviderQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.HasDraft || result.DraftData == null)
        {
            return NotFound(new
            {
                success = false,
                message = "No draft provider found",
                errorCode = "DRAFT_NOT_FOUND"
            });
        }

        var response = new GetDraftProviderResponse
        {
            ProviderId = result.ProviderId!.Value,
            RegistrationStep = result.RegistrationStep!.Value,
            BusinessName = result.DraftData.BusinessName,
            BusinessDescription = result.DraftData.BusinessDescription,
            Category = result.DraftData.Category,
            PhoneNumber = result.DraftData.PhoneNumber,
            Email = result.DraftData.Email,
            AddressLine1 = result.DraftData.AddressLine1,
            AddressLine2 = result.DraftData.AddressLine2,
            City = result.DraftData.City,
            Province = result.DraftData.Province,
            PostalCode = result.DraftData.PostalCode,
            Latitude = result.DraftData.Latitude,
            Longitude = result.DraftData.Longitude
        };

        _logger.LogInformation("Draft provider {ProviderId} retrieved at step {Step}",
            result.ProviderId, result.RegistrationStep);

        return Ok(response);
    }

    /// <summary>
    /// Completes provider registration (Final step - Step 9)
    /// </summary>
    /// <remarks>
    /// Finalizes the registration process and moves provider to PendingVerification status.
    /// Returns new JWT tokens with provider claims.
    /// </remarks>
    /// <param name="providerId">Provider ID to complete</param>
    /// <returns>Completion result with new tokens</returns>
    /// <response code="200">Registration completed successfully</response>
    /// <response code="400">Validation failed or missing required data</response>
    /// <response code="404">Provider not found</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">Not authorized to complete this registration</response>
    [HttpPost("{providerId}/complete")]
    [Authorize]
    [Booksy.API.Middleware.EnableRateLimiting("provider-registration")]
    [ProducesResponseType(typeof(CompleteProviderRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CompleteProviderRegistration(
        [FromRoute] Guid providerId,
        CancellationToken cancellationToken = default)
    {
        var command = new CompleteProviderRegistrationCommand(providerId);
        var result = await _mediator.Send(command, cancellationToken);

        var response = new CompleteProviderRegistrationResponse
        {
            ProviderId = result.ProviderId,
            Status = result.Status,
            Message = result.Message,
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken
        };

        _logger.LogInformation("Provider {ProviderId} registration completed with status {Status}",
            result.ProviderId, result.Status);

        return Ok(response);
    }

    #endregion

    /// <summary>
    /// Registers a new service provider (simple registration)
    /// </summary>
    /// <remarks>
    /// Requires authentication. The OwnerId is automatically set from the authenticated user's ID.
    /// Users must first register in the User Management system before creating a provider profile.
    /// </remarks>
    /// <param name="request">Provider registration details</param>
    /// <returns>Created provider information</returns>
    /// <response code="201">Provider successfully created</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="409">Provider already exists for this user</response>
    [HttpPost("register")]
    [Authorize]
    [Booksy.API.Middleware.EnableRateLimiting("provider-registration")]
    [ProducesResponseType(typeof(ProviderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterProvider(
        [FromBody] RegisterProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get OwnerId from authenticated user - cross-bounded-context integration point
        var currentUserId = GetCurrentUserId();

        // Override any OwnerId in request with authenticated user's ID
        // This ensures users can only create providers for themselves
        request.OwnerId = Guid.Parse(currentUserId);

        var command = MapToRegisterCommand(request);
        var result = await _mediator.Send(command, cancellationToken);

        var response = MapToProviderResponse(result);

        _logger.LogInformation("Provider {ProviderId} registered successfully for user {UserId}",
            response.Id, currentUserId);

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
    /// Get provider business information
    /// </summary>
    [HttpGet("by-owner/{id:guid}")]
    //[Authorize]
    [ProducesResponseType(typeof(ProviderDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProviderByOwnerId(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        //var currentUserId = GetCurrentUserId();
        //if (string.IsNullOrEmpty(currentUserId))
        //{
        //    return Unauthorized();
        //}

        var query = new GetProviderByOwnerIdQuery(id);
        var provider = await _mediator.Send(query, cancellationToken);

        if (provider == null)
        {
            return NotFound();
        }

        return Ok(MapToProviderDetailsResponse(provider));
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

        if (result == null)
        {
            return NotFound();
        }
        var mapped = MapToProviderDetailsResponse(result);
        return Ok(mapped);
    }

    /// <summary>
    /// Gets the current authenticated user's Provider status
    /// </summary>
    /// <returns>Provider status information</returns>
    /// <response code="200">Provider status retrieved successfully</response>
    /// <response code="404">Provider record not found for current user</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("current/status")]
    [Authorize]
    [ProducesResponseType(typeof(ProviderStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentProviderStatus(
        CancellationToken cancellationToken = default)
    {
        var query = new GetCurrentProviderStatusQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Provider record not found",
                errorCode = "PROVIDER_NOT_FOUND"
            });
        }

        var response = new ProviderStatusResponse
        {
            ProviderId = result.ProviderId,
            Status = result.Status.ToString(),
            UserId = result.UserId
        };

        _logger.LogInformation("Provider status retrieved for user {UserId}: {Status}",
            result.UserId, result.Status);

        return Ok(response);
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
                FormattedAddress = locationResult.Address.FormattedAddress,
                City = locationResult.Address.City,
                State = locationResult.Address.State,
                PostalCode = locationResult.Address.PostalCode,
                Country = locationResult.Address.Country,
                ProvinceId = locationResult.Address.ProvinceId,
                CityId = locationResult.Address.CityId,
                Latitude = locationResult.Address.Latitude,
                Longitude = locationResult.Address.Longitude
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

    #region Staff Management

    /// <summary>
    /// Gets all staff members for a provider
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <param name="activeOnly">Return only active staff members</param>
    /// <returns>List of staff members</returns>
    /// <response code="200">Staff members retrieved successfully</response>
    /// <response code="404">Provider not found</response>
    /// <response code="403">Not authorized to view this provider's staff</response>
    [HttpGet("{id:guid}/staff")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<StaffSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStaff(
        [FromRoute] Guid id,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageProvider(id))
        {
            return Forbid();
        }

        // ✅ DDD-Compliant: Use GetProviderStaffQuery which accesses staff through Provider aggregate
        var query = new GetProviderStaffQuery(id, !activeOnly); // Note: inverted logic - query param is "IncludeInactive"
        var result = await _mediator.Send(query, cancellationToken);

        var response = result.Staff.Select(staff => new StaffSummaryResponse
        {
            Id = staff.Id,
            ProviderId = result.ProviderId,
            FirstName = staff.FirstName,
            LastName = staff.LastName,
            FullName = staff.FullName,
            PhoneNumber = staff.PhoneNumber,
            Role = staff.Role,
            IsActive = staff.IsActive,
            HiredAt = staff.HiredAt,
            Biography = staff.Biography,
            ProfilePhotoUrl = staff.ProfilePhotoUrl
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Adds a new staff member to the provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="request">Staff member details</param>
    /// <returns>Created staff member information</returns>
    /// <response code="201">Staff member created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Provider not found</response>
    /// <response code="403">Not authorized to add staff to this provider</response>
    /// <response code="409">Staff member with email already exists</response>
    [HttpPost("{providerId:guid}/staff")]
    [Authorize]
    [ProducesResponseType(typeof(StaffDetailsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddStaff(
        [FromRoute] Guid providerId,
        [FromBody] AddStaffRequest request,
        CancellationToken cancellationToken = default)
    {
  

        var command = new AddStaffToProviderCommand(
            providerId,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.CountryCode,
            request.Role,
            request.Notes,
            request.Biography,
            request.ProfilePhotoUrl);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Staff member {StaffId} added to provider {ProviderId}", result.StaffId, providerId);

        var response = new StaffDetailsResponse
        {
            Id = result.StaffId,
            ProviderId = result.ProviderId,
            FirstName = result.FirstName,
            LastName = result.LastName,
            Role = result.Role,
            IsActive = result.IsActive,
        };

        return CreatedAtAction(nameof(GetStaff), new { id = providerId }, response);
    }

    /// <summary>
    /// Updates an existing staff member
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <param name="staffId">Staff member ID</param>
    /// <param name="request">Updated staff member details</param>
    /// <returns>Updated staff member information</returns>
    /// <response code="200">Staff member updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Staff member not found</response>
    /// <response code="403">Not authorized to update this staff member</response>
    [HttpPut("{id:guid}/staff/{staffId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(StaffDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStaff(
        [FromRoute] Guid id,
        [FromRoute] Guid staffId,
        [FromBody] UpdateStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageProvider(id))
        {
            return Forbid();
        }

        // ✅ DDD-Compliant: Use UpdateProviderStaffCommand which operates on Provider aggregate
        var command = new UpdateProviderStaffCommand(
            id,
            staffId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.CountryCode,
            request.Role,
            request.Notes,
            request.Biography,
            request.ProfilePhotoUrl);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Staff member {StaffId} updated for provider {ProviderId}", staffId, id);

        var response = new StaffDetailsResponse
        {
            Id = result.StaffId,
            ProviderId = result.ProviderId,
            FirstName = result.FirstName,
            LastName = result.LastName,
            Email = result.Email,
            Role = result.Role,
            IsActive = result.IsActive,
        };

        return Ok(response);
    }

    /// <summary>
    /// Removes a staff member from the provider
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <param name="staffId">Staff member ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Staff member removed successfully</response>
    /// <response code="404">Staff member not found</response>
    /// <response code="403">Not authorized to remove this staff member</response>
    [HttpDelete("{id:guid}/staff/{staffId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveStaff(
        [FromRoute] Guid id,
        [FromRoute] Guid staffId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageProvider(id))
        {
            return Forbid();
        }

        // ✅ DDD-Compliant: Use DeactivateProviderStaffCommand which operates on Provider aggregate
        var command = new DeactivateProviderStaffCommand(
            id,
            staffId,
            "Removed from provider");

        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Staff member {StaffId} removed from provider {ProviderId}", staffId, id);

        return NoContent();
    }

    /// <summary>
    /// Upload profile photo for a staff member
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <param name="staffId">Staff member ID</param>
    /// <param name="file">Photo file to upload</param>
    /// <returns>Upload result with image URLs</returns>
    /// <response code="200">Photo uploaded successfully</response>
    /// <response code="400">Invalid file or request</response>
    /// <response code="404">Staff member not found</response>
    /// <response code="403">Not authorized to manage this staff member</response>
    [HttpPost("{id:guid}/staff/{staffId:guid}/photo")]
    [Authorize]
    [ProducesResponseType(typeof(UploadImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadStaffPhoto(
        [FromRoute] Guid id,
        [FromRoute] Guid staffId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageProvider(id))
        {
            return Forbid();
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { message = "Invalid file type. Only JPG, PNG, and WebP are allowed" });
        }

        // Validate file size (5MB max)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { message = "File size exceeds 5MB limit" });
        }

        try
        {
            // Upload using existing image storage service
            var imageUrl = await _imageStorageService.SaveProfileImageAsync(id, file);

            // Update staff profile photo URL via command
            var command = new UpdateProviderStaffCommand(
                id,
                staffId,
                null, // firstName - not updating
                null, // lastName - not updating
                null, // email - not updating
                null, // phone - not updating
                null, // countryCode - not updating
                null, // role - not updating
                null, // notes - not updating
                null, // biography - not updating
                imageUrl); // profilePhotoUrl

            var updatedStaff = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Staff photo uploaded for staff {StaffId} in provider {ProviderId}", staffId, id);

            return Ok(new UploadImageResponse
            {
                ImageUrl = imageUrl,
                ThumbnailUrl = imageUrl // For now, same URL - can add thumbnail generation later
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading staff photo for staff {StaffId} in provider {ProviderId}", staffId, id);
            return BadRequest(new { message = "Failed to upload photo", error = ex.Message });
        }
    }

    #endregion

    #region Private Helper Methods

    private RegisterProviderCommand MapToRegisterCommand(RegisterProviderRequest request)
    {
        return new RegisterProviderCommand(
            OwnerId: request.OwnerId,
            BusinessName: request.BusinessName,
            Description: request.Description,
            Email: request.ContactInfo.Email,
            ProviderType: request.Type,
            PrimaryPhone: request.ContactInfo.PrimaryPhone,
            SecondaryPhone: request.ContactInfo.SecondaryPhone, Website: request.WebsiteUrl, request.Address.Street, request.Address.City,
            request.Address.State, request.Address.PostalCode, request.Address.Country, request.Address.Longitude, request.Address.Latitude
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
            RegisteredAt = result.RegisteredAt,
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn
        };
    }

    private ProviderDetailsResponse MapToProviderDetailsResponse(ProviderDetailsViewModel result)
    {
        return new ProviderDetailsResponse
        {
           
            Id = result.Id,
            OwnerId = result.OwnerId,
            BusinessName = result.BusinessName,
            ProfileImageUrl = result.ProfileImageUrl,
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
                FormattedAddress = result.Address.Street,
                City = result.Address.City,
                State = result.Address.State,
                PostalCode = result.Address.PostalCode,
                Country = result.Address.Country,
                ProvinceId = result.Address.ProvinceId,
                CityId = result.Address.CityId,
                Latitude = result.Address.Latitude,
                Longitude = result.Address.Longitude
            },
            BusinessHours = result.BusinessHours.Any() ? result.BusinessHours.ToDictionary(
                bh => bh.Key,
                bh => bh.Value != null ? new BusinessHoursResponse
                {
                    OpenTime = bh.Value.OpenTime,
                    CloseTime = bh.Value.CloseTime
                } : null) : new Dictionary<Domain.Enums.DayOfWeek, BusinessHoursResponse?>(),
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
            Staff = result.Staff?.Select(c => new StaffMemberResponse
            {
                FirstName = c.FirstName,
                LastName = c.LastName,
                Id = c.Id,
                IsActive = c.IsActive,
                JoinedAt = c.HiredAt,
                Role = c.Role.ToString(),
            }).ToList() ?? new List<StaffMemberResponse>(),
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

    #region Gallery Management

    /// <summary>
    /// Upload images to provider gallery
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="files">Image files to upload (max 10 files, 10MB each)</param>
    /// <returns>List of uploaded gallery images</returns>
    /// <response code="200">Images uploaded successfully</response>
    /// <response code="400">Invalid files or validation errors</response>
    /// <response code="404">Provider not found</response>
    [HttpPost("{providerId}/gallery")]
    [Authorize]
    [RequestSizeLimit(52428800)] // 50MB for multiple files
    [ProducesResponseType(typeof(List<GalleryImageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadGalleryImages(
        [FromRoute] Guid providerId,
        [FromForm] IFormFileCollection files,
        CancellationToken cancellationToken = default)
    {
        var command = new Application.Commands.Provider.UploadGalleryImages.UploadGalleryImagesCommand(
            providerId,
            files);

        var result = await _mediator.Send(command, cancellationToken);

        var response = result.Select(dto => new GalleryImageResponse
        {
            Id = dto.Id,
            ThumbnailUrl = dto.ThumbnailUrl,
            MediumUrl = dto.MediumUrl,
            OriginalUrl = dto.OriginalUrl,
            DisplayOrder = dto.DisplayOrder,
            Caption = dto.Caption,
            AltText = dto.AltText,
            UploadedAt = dto.UploadedAt,
            IsPrimary = dto.IsPrimary
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get all gallery images for a provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <returns>List of gallery images sorted by display order</returns>
    /// <response code="200">Gallery images retrieved successfully</response>
    /// <response code="404">Provider not found</response>
    [HttpGet("{providerId}/gallery")]
    [Authorize]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GalleryImageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGalleryImages(
        [FromRoute] Guid providerId,
        CancellationToken cancellationToken = default)
    {
        var query = new Application.Queries.Provider.GetGalleryImages.GetGalleryImagesQuery(providerId);
        var result = await _mediator.Send(query, cancellationToken);

        var response = result.Select(dto => new GalleryImageResponse
        {
            Id = dto.Id,
            ThumbnailUrl = dto.ThumbnailUrl,
            MediumUrl = dto.MediumUrl,
            OriginalUrl = dto.OriginalUrl,
            DisplayOrder = dto.DisplayOrder,
            Caption = dto.Caption,
            AltText = dto.AltText,
            UploadedAt = dto.UploadedAt,
            IsPrimary = dto.IsPrimary
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Update gallery image metadata (caption, alt text)
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="imageId">Image ID</param>
    /// <param name="request">Updated metadata</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Metadata updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Provider or image not found</response>
    [HttpPut("{providerId}/gallery/{imageId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGalleryImageMetadata(
        [FromRoute] Guid providerId,
        [FromRoute] Guid imageId,
        [FromBody] UpdateGalleryImageMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new Application.Commands.Provider.UpdateGalleryImageMetadata.UpdateGalleryImageMetadataCommand(
            providerId,
            imageId,
            request.Caption,
            request.AltText);

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Reorder gallery images
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="request">New image order mapping</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Images reordered successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Provider not found</response>
    [HttpPut("{providerId}/gallery/reorder")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderGalleryImages(
        [FromRoute] Guid providerId,
        [FromBody] ReorderGalleryImagesRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new Application.Commands.Provider.ReorderGalleryImages.ReorderGalleryImagesCommand(
            providerId,
            request.ImageOrders);

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Set a gallery image as primary/featured
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="imageId">Image ID to set as primary</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Image set as primary successfully</response>
    /// <response code="404">Provider or image not found</response>
    [HttpPut("{providerId}/gallery/{imageId}/set-primary")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryGalleryImage(
        [FromRoute] Guid providerId,
        [FromRoute] Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var command = new Application.Commands.Provider.SetPrimaryGalleryImage.SetPrimaryGalleryImageCommand(
            providerId,
            imageId);

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Delete a gallery image
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="imageId">Image ID to delete</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Image deleted successfully</response>
    /// <response code="404">Provider or image not found</response>
    [HttpDelete("{providerId}/gallery/{imageId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGalleryImage(
        [FromRoute] Guid providerId,
        [FromRoute] Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var command = new Application.Commands.Provider.DeleteGalleryImage.DeleteGalleryImageCommand(
            providerId,
            imageId);

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Upload profile image for the provider
    /// </summary>
    /// <param name="image">Profile image file (max 5MB, jpg/png/webp)</param>
    /// <returns>URL of uploaded profile image</returns>
    /// <response code="200">Profile image uploaded successfully</response>
    /// <response code="400">Invalid image file or validation errors</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("profile/image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5242880)] // 5MB
    [ProducesResponseType(typeof(UploadImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadProfileImage(
        IFormFile image,
        CancellationToken cancellationToken = default)
    {
        if (image == null || image.Length == 0)
        {
            return BadRequest(new ApiErrorResult("No image file provided")
            {
                Errors = new Dictionary<string, string[]>
                {
                    { "image", new[] { "Image file is required" } }
                }
            });
        }

        if (!_imageStorageService.IsValidImageType(image))
        {
            return BadRequest(new ApiErrorResult("Invalid image format")
            {
                Errors = new Dictionary<string, string[]>
                {
                    { "image", new[] { "Only JPG, PNG, GIF, and WebP images are allowed" } }
                }
            });
        }

        // Get current user ID and find their provider
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            return Unauthorized();
        }

        // Query for provider by owner ID
        var query = new GetProviderByOwnerIdQuery(userId);
        var provider = await _mediator.Send(query, cancellationToken);

        if (provider == null)
        {
            return NotFound(new ApiErrorResult("Provider not found for current user"));
        }

        var imageUrl = await _imageStorageService.SaveProfileImageAsync(provider.Id, image);

        _logger.LogInformation("Profile image uploaded for provider {ProviderId}: {ImageUrl}", provider.Id, imageUrl);

        return Ok(new UploadImageResponse
        {
            ImageUrl = imageUrl
        });
    }

    /// <summary>
    /// Upload business logo for the provider
    /// </summary>
    /// <param name="image">Business logo image file (max 5MB, jpg/png/webp)</param>
    /// <returns>URL of uploaded business logo</returns>
    /// <response code="200">Business logo uploaded successfully</response>
    /// <response code="400">Invalid image file or validation errors</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("business/logo")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5242880)] // 5MB
    [ProducesResponseType(typeof(UploadImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadBusinessLogo(
        IFormFile image,
        CancellationToken cancellationToken = default)
    {
        if (image == null || image.Length == 0)
        {
            return BadRequest(new ApiErrorResult("No image file provided")
            {
                Errors = new Dictionary<string, string[]>
                {
                    { "image", new[] { "Image file is required" } }
                }
            });
        }

        if (!_imageStorageService.IsValidImageType(image))
        {
            return BadRequest(new ApiErrorResult("Invalid image format")
            {
                Errors = new Dictionary<string, string[]>
                {
                    { "image", new[] { "Only JPG, PNG, GIF, and WebP images are allowed" } }
                }
            });
        }

        // Get current user ID and find their provider
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            return Unauthorized();
        }

        // Query for provider by owner ID
        var query = new GetProviderByOwnerIdQuery(userId);
        var provider = await _mediator.Send(query, cancellationToken);

        if (provider == null)
        {
            return NotFound(new ApiErrorResult("Provider not found for current user"));
        }

        var imageUrl = await _imageStorageService.SaveBusinessLogoAsync(provider.Id, image);

        _logger.LogInformation("Business logo uploaded for provider {ProviderId}: {ImageUrl}", provider.Id, imageUrl);

        return Ok(new UploadImageResponse
        {
            ImageUrl = imageUrl
        });
    }

    /// <summary>
    /// Update provider profile information
    /// </summary>
    /// <param name="request">Updated profile information</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Profile updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Provider not found</response>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get current user ID and find their provider
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            return Unauthorized();
        }

        // Query for provider by owner ID
        var query = new GetProviderByOwnerIdQuery(userId);
        var provider = await _mediator.Send(query, cancellationToken);

        if (provider == null)
        {
            return NotFound(new ApiErrorResult("Provider not found for current user"));
        }

        var command = new Application.Commands.Provider.UpdateProviderProfile.UpdateProviderProfileCommand(
            provider.Id,
            request.FullName,
            request.Email,
            request.ProfileImageUrl);

        await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Profile updated for provider {ProviderId}", provider.Id);

        return NoContent();
    }

    /// <summary>
    /// Update provider business information
    /// </summary>
    /// <param name="request">Updated business information</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Business information updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Provider not found</response>
    [HttpPut("business")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBusinessInfo(
        [FromBody] UpdateBusinessInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get current user ID and find their provider
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            return Unauthorized();
        }

        // Query for provider by owner ID
        var query = new GetProviderByOwnerIdQuery(userId);
        var provider = await _mediator.Send(query, cancellationToken);

        if (provider == null)
        {
            return NotFound(new ApiErrorResult("Provider not found for current user"));
        }

        var command = new UpdateBusinessProfileCommand(
            ProviderId: provider.Id,
            BusinessName: request.BusinessName,
            Description: request.Description);

        var result = await _mediator.Send(command, cancellationToken);

        // Update logo if provided
        if (!string.IsNullOrWhiteSpace(request.LogoUrl))
        {
            var updateLogoCommand = new Application.Commands.Provider.UpdateBusinessLogo.UpdateBusinessLogoCommand(
                provider.Id,
                request.LogoUrl);

            await _mediator.Send(updateLogoCommand, cancellationToken);
        }

        _logger.LogInformation("Business information updated for provider {ProviderId}", provider.Id);

        return NoContent();
    }

    #endregion
}

#region Progressive Registration Request/Response Models

public sealed class CreateProviderDraftRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}

public sealed class CreateProviderDraftResponse
{
    public Guid ProviderId { get; set; }
    public int RegistrationStep { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class GetDraftProviderResponse
{
    public Guid ProviderId { get; set; }
    public int RegistrationStep { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}

public sealed class CompleteProviderRegistrationResponse
{
    public Guid ProviderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}

#endregion

#region Staff Request/Response Models

public sealed class AddStaffRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? CountryCode { get; set; }
    public string Role { get; set; } = "ServiceProvider";
    public string? Notes { get; set; }
    public string? Biography { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public sealed class UpdateStaffRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? CountryCode { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Biography { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public sealed class StaffDetailsResponse
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public string? TerminationReason { get; set; }
    public string? Notes { get; set; }
    public string? Biography { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime HiredAt { get; set; }
}

public sealed class StaffSummaryResponse
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime HiredAt { get; set; }
    public string? Biography { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

#endregion

#region Image Upload Models

public sealed class UploadImageResponse
{
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}

public sealed class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? ProfileImageUrl { get; set; }
}

#endregion
