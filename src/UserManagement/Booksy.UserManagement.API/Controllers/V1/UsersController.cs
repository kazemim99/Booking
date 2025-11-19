using Booksy.Core.Application.DTOs;
using Booksy.UserManagement.API.Models.Requests;
using Booksy.UserManagement.API.Models.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using Booksy.UserManagement.Application.CQRS.Commands.ActivateUser;
using Booksy.UserManagement.Application.CQRS.Commands.ChangePassword;
using Booksy.UserManagement.Application.CQRS.Commands.RegisterUser;
using Booksy.UserManagement.Application.CQRS.Queries.GetUserById;
using Booksy.UserManagement.Application.DTOs;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Application.CQRS.Commands.UpldateUserProfile;
using Booksy.API.Extensions;
using Booksy.API.Middleware;
using Booksy.Core.Domain.Exceptions;
using Booksy.UserManagement.Application.CQRS.Queries.GetUsersByStatus;
using Booksy.UserManagement.Application.CQRS.Commands.DeActivateUser;
using Booksy.UserManagement.Application.CQRS.Commands.DeleteUser;

namespace Booksy.UserManagement.API.Controllers.V1;

/// <summary>
/// Manages user accounts and profiles
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ISender mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new user account
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Created user information</returns>
    /// <response code="201">User successfully created</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">User already exists</response>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("registration")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterUser(
        [FromBody][Required] RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {

        var command = MapToRegisterCommand(request);
        var response = await _mediator.Send(command, cancellationToken);


        return CreatedAtAction(
            nameof(GetUserById),
            new { id = response.UserId, version = "1.0" },
            response);
    }

    /// <summary>
    /// Gets a user by their ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    /// <response code="200">User found</response>
    /// <response code="404">User not found</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized to view this user</response>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(UserDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        // Check if user can view this profile
        if (!await CanAccessUserProfile(id))
        {
            _logger.LogWarning("User {RequestingUser} attempted to access user {UserId} without permission",
                GetCurrentUserId(), id);
            return Forbid();
        }

        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);


        return Ok(MapToUserDetailsResponse(result));
    }

    /// <summary>
    /// Search users with advanced filtering and pagination
    /// </summary>
    /// <param name="request">Search parameters including pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results</returns>
    /// <response code="200">Users found successfully</response>
    /// <response code="400">Invalid search parameters</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (admin only)</response>
    [HttpGet("search")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResult<SearchUsersResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<SearchUsersResponse>>> SearchUsers(
        [FromQuery] SearchUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        // Convert API request to application query (single line!)
        var query = request.ToQuery();

        // Execute query through MediatR
        var result = await _mediator.Send(query, cancellationToken);

        // Transform to API response model
        var response = result.Map(searchResult => new SearchUsersResponse
        {
            Id = searchResult.UserId,
            Email = searchResult.Email,
            FirstName = searchResult.FirstName,
            LastName = searchResult.LastName,
            FullName = searchResult.FullName,
            PhoneNumber = searchResult.PhoneNumber,
            Status = searchResult.Status.ToString(),
            Type = searchResult.Type.ToString(),
            IsLocked = searchResult.IsLocked,
            TwoFactorEnabled = searchResult.TwoFactorEnabled,
            AvatarUrl = searchResult.AvatarUrl,
            City = searchResult.City,
            Country = searchResult.Country,
            RegisteredAt = searchResult.RegisteredAt,
            ActivatedAt = searchResult.ActivatedAt,
            LastLoginAt = searchResult.LastLoginAt,
            FailedLoginAttempts = searchResult.FailedLoginAttempts,
            Roles = searchResult.Roles.Select(r => new UserRoleResponse
            {
                Name = r.Name,
                AssignedAt = r.AssignedAt,
                ExpiresAt = r.ExpiresAt,
                IsExpired = r.IsExpired
            }).ToList(),
            PrimaryRole = searchResult.PrimaryRole,
            DisplayName = searchResult.DisplayName,
            StatusDescription = searchResult.StatusDescription
        });

        // Return with proper pagination headers
        return this.PaginatedOk(response);
    }


    /// <summary>
    /// Activates a user account
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Activation details</param>
    /// <returns>Success message</returns>
    /// <response code="200">Account activated successfully</response>
    /// <response code="400">Invalid activation token or user already active</response>
    /// <response code="404">User not found</response>
    [HttpPost("{id:guid}/activate")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActivateUser(
        [FromRoute] Guid id,
        [FromBody][Required] ActivateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ActivateUserCommand(id, request.ActivationToken);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(new MessageResponse("Account activated successfully"));
    }

    /// <summary>
    /// Updates a user's profile
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Updated profile information</param>
    /// <returns>Updated user information</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid update data</response>
    /// <response code="404">User not found</response>
    /// <response code="403">Not authorized to update this profile</response>
    [HttpPut("{id:guid}/profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUserProfile(
        [FromRoute] Guid id,
        [FromBody][Required] UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        // Users can only update their own profile unless they're an admin
        if (!await CanModifyUser(id))
        {
            _logger.LogWarning("User {RequestingUser} attempted to update user {UserId} without permission",
                GetCurrentUserId(), id);
            return Forbid();
        }

        var command = new UpdateUserProfileCommand(
            id,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Bio,
            request.Address,
            request.ProfilePictureUrl);

        var response = await _mediator.Send(command, cancellationToken);



        _logger.LogInformation("User {UserId} profile updated successfully", id);
        return CreatedAtAction(
                  nameof(GetUserById),
                  new { id, version = "1.0" },
                  response);
    }

    /// <summary>
    /// Changes a user's password
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Password change details</param>
    /// <returns>Success message</returns>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Invalid password or validation failed</response>
    /// <response code="401">Current password incorrect</response>
    /// <response code="403">Not authorized to change this password</response>
    [HttpPost("{id:guid}/change-password")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangePassword(
        [FromRoute] Guid id,
        [FromBody][Required] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        // Users can only change their own password unless they're an admin
        if (!await CanModifyUser(id))
        {
            _logger.LogWarning("User {RequestingUser} attempted to change password for user {UserId} without permission",
                GetCurrentUserId(), id);
            return Forbid();
        }

        var command = new ChangePasswordCommand(id, request.CurrentPassword, request.NewPassword);
        await _mediator.Send(command, cancellationToken);



        return Ok(new MessageResponse("Password changed successfully"));
    }

    /// <summary>
    /// Deactivates a user account (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Deactivation reason</param>
    /// <returns>Success message</returns>
    /// <response code="200">Account deactivated successfully</response>
    /// <response code="404">User not found</response>
    /// <response code="403">Not authorized (admin only)</response>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateUser(
        [FromRoute] Guid id,
        [FromBody] DeactivateUserRequest? request,
        CancellationToken cancellationToken = default)
    {
        var command = new DeActivateUserCommand(id, request?.Reason ?? "Deactivated by administrator");
        var result = await _mediator.Send(command, cancellationToken);



        _logger.LogInformation("User {UserId} deactivated by admin {AdminId}", id, GetCurrentUserId());
        return Ok(new MessageResponse("Account deactivated successfully"));
    }

    /// <summary>
    /// Permanently deletes a user account (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success message</returns>
    /// <response code="204">Account deleted successfully</response>
    /// <response code="404">User not found</response>
    /// <response code="403">Not authorized (admin only)</response>
    /// <response code="409">Cannot delete user with active data</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SysAdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteUserCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogWarning("User {UserId} permanently deleted by admin {AdminId}", id, GetCurrentUserId());
        return NoContent();
    }

    /// <summary>
    /// Gets users filtered by status
    /// </summary>
    /// <param name="status">User status to filter by</param>
    /// <param name="maxResults">Maximum number of results (default: 100, max: 1000)</param>
    /// <returns>List of users with the specified status</returns>
    /// <response code="200">Users retrieved successfully</response>
    /// <response code="400">Invalid status provided</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (admin only)</response>
    [HttpGet("by-status/{status}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<GetUsersByStatusResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsersByStatus(
        [FromRoute] UserStatus status,
        [FromQuery] int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersByStatusQuery(status, maxResults);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }


    #region Private Helper Methods

    private RegisterUserCommand MapToRegisterCommand(RegisterUserRequest request)
    {
        return new RegisterUserCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.UserType,
            request.AcceptTerms,
            request.MarketingConsent,
            request.ReferralCode);
    }

    private UserDetailsResponse MapToUserDetailsResponse(UserDetailsViewModel user)
    {
        return new UserDetailsResponse
        {
            Id = user.UserId,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Status = user.Status,
            CreatedAt = user.RegisteredAt,
            Bio = user.Bio,
            
            Roles = user.Roles.Select(c=>c.Name).ToList(),
            RegisteredAt = user.RegisteredAt,
            LastLoginAt = user.LastLoginAt,
            TwoFactorEnabled = user.TwoFactorEnabled,
            Profile = new UserProfileResponse
            {
                Bio  = user.Bio,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarUrl = user.AvatarUrl,
            }
        };
    }



    private string? GetCurrentUserId()
    {
        return User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
    }

    private async Task<bool> CanAccessUserProfile(Guid userId)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return false;

        // Users can view their own profile
        if (currentUserId == userId.ToString())
            return true;

        // Admins can view any profile
        if (User.IsInRole("Admin") || User.IsInRole("SysAdmin"))
            return true;

        // Additional business logic could go here (e.g., providers viewing client profiles for appointments)
        return false;
    }

    private async Task<bool> CanModifyUser(Guid userId)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return false;

        // Users can modify their own data
        if (currentUserId == userId.ToString())
            return true;

        // Only admins can modify other users
        return User.IsInRole("Admin") || User.IsInRole("SysAdmin");
    }

    #endregion
}

