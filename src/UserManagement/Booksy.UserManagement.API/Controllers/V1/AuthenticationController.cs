// ========================================
// Controllers/V1/UsersController.cs
// ========================================
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity.Data;
using Booksy.UserManagement.API.Models.Responses;
using Booksy.UserManagement.API.Models.Requests;
using Booksy.UserManagement.Application.CQRS.Commands.AuthenticateUser;
using LoginRequest = Booksy.UserManagement.API.Models.Requests.LoginRequest;
using Booksy.UserManagement.Application.CQRS.Commands.RefreshToken;
using Booksy.UserManagement.Application.CQRS.Commands.RequestPasswordReset;


namespace Booksy.UserManagement.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public class AuthenticationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(IMediator mediator, ILogger<AuthenticationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and get access token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new AuthenticateUserCommand(
            request.Email,
            request.Password,
            request.RememberMe);

        var result = await _mediator.Send(command);

      

        var response = new AuthenticationResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn,
            TokenType = "Bearer",
            UserInfo = new UserInfoResponse
            {
                Id = result.UserId,
                Email = result.Email,
                DisplayName = result.DisplayName,
                Roles = result.Roles
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await _mediator.Send(command);

    

        var response = new AuthenticationResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn,
            TokenType = "Bearer"
        };

        return Ok(response);
    }

    /// <summary>
    /// Logout user
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        // In a real implementation, you would invalidate the refresh token
        var userId = User.FindFirst("sub")?.Value;
        _logger.LogInformation("User {UserId} logged out", userId);

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("password-reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] Models.Requests.ForgotPasswordRequest request)
    {
        var command = new RequestPasswordResetCommand(request.Email);
        await _mediator.Send(command);

        // Always return success to prevent email enumeration
        return Ok(new { message = "If the email exists, a password reset link has been sent." });
    }
}

