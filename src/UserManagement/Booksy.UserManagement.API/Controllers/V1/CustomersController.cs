using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Booksy.UserManagement.Application.CQRS.Commands.Customer.RegisterCustomer;
using Booksy.UserManagement.Application.CQRS.Commands.Customer.UpdateCustomerProfile;
using Booksy.UserManagement.Application.CQRS.Commands.Customer.AddFavoriteProvider;
using Booksy.UserManagement.Application.CQRS.Commands.Customer.RemoveFavoriteProvider;
using Booksy.UserManagement.Application.CQRS.Commands.Customer.UpdateNotificationPreferences;
using Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerById;
using Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerFavoriteProviders;
using Booksy.UserManagement.Application.CQRS.Queries.Customer.GetCustomerProfile;
using Booksy.UserManagement.Application.CQRS.Queries.Customer.GetUpcomingBookings;

namespace Booksy.UserManagement.API.Controllers.V1;

/// <summary>
/// Manages customer accounts and profiles
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ISender mediator, ILogger<CustomersController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new customer account
    /// </summary>
    /// <param name="request">Customer registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created customer information</returns>
    /// <response code="201">Customer successfully created</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">Customer already exists</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("registration")]
    [ProducesResponseType(typeof(RegisterCustomerResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterCustomer(
        [FromBody][Required] RegisterCustomerCommand request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetCustomerById),
                new { id = result.CustomerId, version = "1.0" },
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering customer");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while registering the customer" });
        }
    }

    /// <summary>
    /// Gets a customer by their ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer details</returns>
    /// <response code="200">Customer found</response>
    /// <response code="404">Customer not found</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized to view this customer</response>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Customer,Admin")]
    [ProducesResponseType(typeof(CustomerDetailsViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCustomerById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user can access this customer profile
            if (!await CanAccessCustomerProfile(id))
            {
                _logger.LogWarning("User {RequestingUser} attempted to access customer {CustomerId} without permission",
                    GetCurrentUserId(), id);
                return Forbid();
            }

            var query = new GetCustomerByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer not found: {CustomerId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the customer" });
        }
    }

    /// <summary>
    /// Updates customer profile information
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="request">Updated profile information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated customer information</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Customer not found</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized to update this profile</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(UpdateCustomerProfileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCustomerProfile(
        [FromRoute] Guid id,
        [FromBody][Required] UpdateCustomerProfileCommand request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure the ID in the route matches the command
            if (id != request.CustomerId)
            {
                return BadRequest(new { error = "Customer ID mismatch" });
            }

            // Check if user can update this profile
            if (!await CanAccessCustomerProfile(id))
            {
                _logger.LogWarning("User {RequestingUser} attempted to update customer {CustomerId} without permission",
                    GetCurrentUserId(), id);
                return Forbid();
            }

            var result = await _mediator.Send(request, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer not found: {CustomerId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer profile {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the customer profile" });
        }
    }

    /// <summary>
    /// Gets customer's favorite providers
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of favorite providers</returns>
    /// <response code="200">Favorites retrieved successfully</response>
    /// <response code="404">Customer not found</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized</response>
    [HttpGet("{id:guid}/favorites")]
    [Authorize(Roles = "Customer,Admin")]
    [ProducesResponseType(typeof(List<FavoriteProviderViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCustomerFavorites(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user can access this customer's favorites
            if (!await CanAccessCustomerProfile(id))
            {
                _logger.LogWarning("User {RequestingUser} attempted to access customer {CustomerId} favorites without permission",
                    GetCurrentUserId(), id);
                return Forbid();
            }

            var query = new GetCustomerFavoriteProvidersQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer not found: {CustomerId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer favorites {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving favorites" });
        }
    }

    /// <summary>
    /// Adds a provider to customer's favorites
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="providerId">Provider ID to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of adding favorite</returns>
    /// <response code="200">Provider added to favorites</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Customer or provider not found</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized</response>
    [HttpPost("{id:guid}/favorites")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(AddFavoriteProviderResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddFavoriteProvider(
        [FromRoute] Guid id,
        [FromBody][Required] AddFavoriteProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user can update this customer's favorites
            if (!await CanAccessCustomerProfile(id))
            {
                _logger.LogWarning("User {RequestingUser} attempted to update customer {CustomerId} favorites without permission",
                    GetCurrentUserId(), id);
                return Forbid();
            }

            var command = new AddFavoriteProviderCommand(id, request.ProviderId, request.Notes);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer not found: {CustomerId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding favorite provider for customer {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while adding the favorite provider" });
        }
    }

    /// <summary>
    /// Removes a provider from customer's favorites
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="providerId">Provider ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of removing favorite</returns>
    /// <response code="200">Provider removed from favorites</response>
    /// <response code="404">Customer or provider not found</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized</response>
    [HttpDelete("{id:guid}/favorites/{providerId:guid}")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(RemoveFavoriteProviderResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveFavoriteProvider(
        [FromRoute] Guid id,
        [FromRoute] Guid providerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user can update this customer's favorites
            if (!await CanAccessCustomerProfile(id))
            {
                _logger.LogWarning("User {RequestingUser} attempted to update customer {CustomerId} favorites without permission",
                    GetCurrentUserId(), id);
                return Forbid();
            }

            var command = new RemoveFavoriteProviderCommand(id, providerId);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer not found: {CustomerId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing favorite provider for customer {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while removing the favorite provider" });
        }
    }

    /// <summary>
    /// Deactivates a customer account
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    /// <response code="200">Customer deactivated successfully</response>
    /// <response code="404">Customer not found</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Customer,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateCustomer(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user can deactivate this customer
            if (!await CanAccessCustomerProfile(id))
            {
                _logger.LogWarning("User {RequestingUser} attempted to deactivate customer {CustomerId} without permission",
                    GetCurrentUserId(), id);
                return Forbid();
            }

            // TODO: Implement DeactivateCustomerCommand
            // For now, return success
            return Ok(new { message = "Customer deactivation not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating customer {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deactivating the customer" });
        }
    }

    /// <summary>
    /// Gets customer profile with notification preferences
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer profile</returns>
    [HttpGet("{id:guid}/profile")]
    [Authorize(Roles = "Customer,Admin")]
    [ProducesResponseType(typeof(CustomerProfileViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerProfile(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await CanAccessCustomerProfile(id))
            {
                return Forbid();
            }

            var query = new GetCustomerProfileQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer not found: {CustomerId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer profile {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the profile" });
        }
    }

    /// <summary>
    /// Gets customer's upcoming bookings
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="limit">Number of bookings to retrieve (default 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of upcoming bookings</returns>
    [HttpGet("{id:guid}/bookings/upcoming")]
    [Authorize(Roles = "Customer,Admin")]
    [ProducesResponseType(typeof(List<UpcomingBookingViewModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUpcomingBookings(
        [FromRoute] Guid id,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await CanAccessCustomerProfile(id))
            {
                return Forbid();
            }

            var query = new GetUpcomingBookingsQuery(id, limit);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming bookings for customer {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving upcoming bookings" });
        }
    }

    /// <summary>
    /// Updates customer notification preferences
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="request">Notification preferences</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated preferences</returns>
    [HttpPatch("{id:guid}/preferences")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(UpdateNotificationPreferencesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNotificationPreferences(
        [FromRoute] Guid id,
        [FromBody][Required] UpdatePreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await CanAccessCustomerProfile(id))
            {
                return Forbid();
            }

            var command = new UpdateNotificationPreferencesCommand
            {
                CustomerId = id,
                SmsEnabled = request.SmsEnabled,
                EmailEnabled = request.EmailEnabled,
                ReminderTiming = request.ReminderTiming
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer not found: {CustomerId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for customer {CustomerId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating preferences" });
        }
    }

    #region Helper Methods

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private async Task<bool> CanAccessCustomerProfile(Guid customerId)
    {
        // TODO: Implement proper authorization logic
        // For now, allow access if user is authenticated and has Customer role
        // In a real implementation, you'd check if the user owns this customer profile
        return User.Identity?.IsAuthenticated == true;
    }

    #endregion
}

/// <summary>
/// Request model for adding a favorite provider
/// </summary>
public sealed record AddFavoriteProviderRequest(
    Guid ProviderId,
    string? Notes = null);

/// <summary>
/// Request model for updating notification preferences
/// </summary>
public sealed record UpdatePreferencesRequest(
    bool SmsEnabled,
    bool EmailEnabled,
    string ReminderTiming);
