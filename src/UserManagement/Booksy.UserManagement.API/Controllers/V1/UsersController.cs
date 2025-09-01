// ========================================
// Controllers/V1/UsersController.cs
// ========================================
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
using Booksy.UserManagement.Application.CQRS.Queries.GetPaginatedUsers;
using Booksy.UserManagement.Application.CQRS.Queries.GetUserById;
using Booksy.UserManagement.Application.CQRS.Queries.GetUsersByStatus;
using Booksy.UserManagement.Application.CQRS.Queries.SearchUsers;
using Booksy.UserManagement.Application.DTOs;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Application.CQRS.Commands.UpldateUserProfile;

namespace Booksy.UserManagement.API.Controllers.V1;

/// <summary>
/// Manages user accounts and profiles
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ErrorResult), StatusCodes.Status500InternalServerError)]
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
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterUser(
        [FromBody][Required] RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {

        var command = MapToRegisterCommand(request);
        var result = await _mediator.Send(command, cancellationToken);


        var response = MapToUserResponse(result);


        return CreatedAtAction(
            nameof(GetUserById),
            new { id = response.Id, version = "1.0" },
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
    /// Gets a paginated list of users (Admin only)
    /// </summary>
    /// <param name="request">Pagination and filter parameters</param>
    /// <returns>Paginated list of users</returns>
    /// <response code="200">Users retrieved successfully</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (admin only)</response>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResult<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPaginatedUsersQuery(
           request.Status,
            request.UserType)
        {
            PageSize = request.PageSize,
            PageNumber = request.PageNumber,
            SortBy = new List<SortingDescriptor>
               {
                   new SortingDescriptor
                   {
                       Direction = request.SortDirection,
                       FieldName = request.SortBy
                   }
               },
        };

        var result = await _mediator.Send(query, cancellationToken);

        var response = new PagedResult<UserResponse>
        (
            result.Items.Select(MapToUserResponse).ToList(),
            result.TotalCount,
          result.PageNumber,
            result.PageSize
        );

        return Ok(response);
    }

    /// <summary>
    /// Searches for users by name or email (Admin only)
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="maxResults">Maximum number of results (default: 50)</param>
    /// <returns>List of matching users</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (admin only)</response>
    [HttpGet("search")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery][Required] string searchTerm,
        [FromQuery] int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        {
            return BadRequest(new ErrorResult("Search term must be at least 2 characters", "INVALID_SEARCH_TERM"));
        }

        var query = new SearchUsersQuery(searchTerm);
        var result = await _mediator.Send(query, cancellationToken);

        var response = new PagedResult<UserResponse>
             (
                 result.Items.Select(MapToUserResponse).ToList(),
                 result.TotalCount,
               result.PageNumber,
                 result.PageSize
             );
        
        
        return Ok(response);
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
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
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

        var result = await _mediator.Send(command, cancellationToken);



        _logger.LogInformation("User {UserId} profile updated successfully", id);
        return Ok(MapToUserDetailsResponse(result));
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
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
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
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status409Conflict)]
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
    /// Gets users by status (Admin only)
    /// </summary>
    /// <param name="status">User status to filter by</param>
    /// <returns>List of users with the specified status</returns>
    /// <response code="200">Users retrieved successfully</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (admin only)</response>
    [HttpGet("by-status/{status}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsersByStatus(
        [FromRoute] string status,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersByStatusQuery(Enum.Parse<UserStatus>(status));
        var result = await _mediator.Send(query, cancellationToken);

        var response = new PagedResult<UserResponse>
        (
            result.Items.Select(MapToUserResponse).ToList(),
            result.TotalCount,
          result.PageNumber,
            result.PageSize
        );

        return Ok(response);
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

    private UserResponse MapToUserResponse(dynamic result)
    {


        return  new UserResponse
        {
            Id = result.UserId,
            Email = result.Email,
            FirstName = result.FullName,
            Status = result.Status.ToString(),
        };


    
    }

    private UserResponse MapToUserResponse(UserDto user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Status = user.Status.ToString(),
            RegisteredAt = user.RegisteredAt
        };
    }

    private UserDetailsResponse MapToUserDetailsResponse(dynamic user)
    {
        return new UserDetailsResponse
        {
            Id = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Status = user.Status,
            Bio = user.Bio,
            RegisteredAt = user.RegisteredAt,
            LastLoginAt = user.LastLoginAt,
            TwoFactorEnabled = user.TwoFactorEnabled,
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

