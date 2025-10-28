using Booksy.API.Extensions;
using Booksy.API.Middleware;
using Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.SendVerificationCode;
using Booksy.UserManagement.Application.CQRS.Commands.PhoneVerification.VerifyCode;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booksy.UserManagement.API.Controllers.V1;

/// <summary>
/// Authentication controller for passwordless phone verification
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ISender mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Sends verification code to phone number
    /// </summary>
    /// <param name="request">Phone verification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send verification response</returns>
    /// <response code="200">Code sent successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="429">Too many requests (rate limited)</response>
    [HttpPost("send-verification-code")]
    [AllowAnonymous]
    [EnableRateLimiting("phone-verification")]
    [ProducesResponseType(typeof(SendVerificationCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionHandlingMiddleware.ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendVerificationCode(
        [FromBody] SendVerificationCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        var command = new SendVerificationCodeCommand(
            request.PhoneNumber,
            request.CountryCode,
            ipAddress,
            userAgent
        );

        var response = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "Verification code sent to {MaskedPhone}",
            response.MaskedPhoneNumber
        );

        return Ok(response);
    }

    /// <summary>
    /// Verifies phone number with OTP code
    /// </summary>
    /// <param name="request">Verification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification response with JWT tokens</returns>
    /// <response code="200">Code verified successfully</response>
    /// <response code="400">Invalid code</response>
    /// <response code="401">Verification failed</response>
    [HttpPost("verify-code")]
    [AllowAnonymous]
    [EnableRateLimiting("code-verification")]
    [ProducesResponseType(typeof(VerifyCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionHandlingMiddleware.ApiErrorResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionHandlingMiddleware.ApiErrorResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyCode(
        [FromBody] VerifyCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new VerifyCodeCommand(
            request.PhoneNumber,
            request.Code,
            request.UserType ?? "Provider"
        );

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            _logger.LogWarning(
                "Code verification failed for phone ending in: •••{LastDigits}. Remaining attempts: {Remaining}",
                request.PhoneNumber.Substring(Math.Max(0, request.PhoneNumber.Length - 4)),
                response.RemainingAttempts
            );

            return Unauthorized(new
            {
                success = false,
                message = response.ErrorMessage,
                remainingAttempts = response.RemainingAttempts
            });
        }

        _logger.LogInformation(
            "Phone verified successfully for user: {UserId}",
            response.User?.Id
        );

        return Ok(response);
    }

    /// <summary>
    /// Generate new JWT token with additional claims (for cross-context communication)
    /// </summary>
    /// <param name="request">Token generation request</param>
    /// <param name="jwtTokenService">JWT token service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token response</returns>
    /// <response code="200">Token generated successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">User not found</response>
    [HttpPost("generate-token")]
    [Authorize] // Require authentication (from ServiceCatalog with service auth)
    [ProducesResponseType(typeof(GenerateTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateToken(
        [FromBody] GenerateTokenRequest request,
        [FromServices] Application.Services.Interfaces.IJwtTokenService jwtTokenService,
        CancellationToken cancellationToken = default)
    {
        // Validate user ID from request
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            return BadRequest(new { message = "Invalid user ID format" });
        }

        _logger.LogInformation(
            "Generating new token for user {UserId} with additional claims",
            userId);

        try
        {
            // Get user - use mediator or repository depending on your architecture
            var getUserQuery = new Application.CQRS.Queries.GetUserById.GetUserByIdQuery(userId);
            var user = await _mediator.Send(getUserQuery, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for token generation", userId);
                return NotFound(new { message = "User not found" });
            }

            // Extract providerId and providerStatus from additional claims if present
            string? providerId = null;
            string? providerStatus = null;
            if (request.AdditionalClaims != null)
            {
                if (request.AdditionalClaims.TryGetValue("provider_id", out var providerIdValue))
                {
                    providerId = providerIdValue;
                }
                if (request.AdditionalClaims.TryGetValue("provider_status", out var providerStatusValue))
                {
                    providerStatus = providerStatusValue;
                }
            }

            // Parse user type from string
            var userType = Enum.TryParse<Domain.Enums.UserType>(user.Type, out var parsedType)
                ? parsedType
                : Domain.Enums.UserType.Customer;

            // Extract role names from RoleViewModel list
            var roleNames = user.Roles.Select(r => r.Name).ToList();

            // Generate new access token using the correct signature
            var accessToken = jwtTokenService.GenerateAccessToken(
                Core.Domain.ValueObjects.UserId.From(user.UserId),
                userType,
                Core.Domain.ValueObjects.Email.Create(user.Email ?? string.Empty),
                user.DisplayName ?? string.Empty,
                user.Status ?? "Active",
                roleNames,
                providerId,
                providerStatus,
                15 // 15 minutes expiration
            );

            // Generate refresh token (user aggregate handles this)
            var refreshTokenValue = Guid.NewGuid().ToString();

            _logger.LogInformation(
                "Successfully generated new token for user {UserId} with providerId: {ProviderId}",
                userId,
                providerId ?? "none");

            return Ok(new GenerateTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                ExpiresIn = 900, // 15 minutes
                TokenType = "Bearer"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while generating the token" });
        }
    }
}

/// <summary>
/// Request model for sending verification code
/// </summary>
public record SendVerificationCodeRequest(
    string PhoneNumber,
    string CountryCode ="+98"
);

/// <summary>
/// Request model for verifying code
/// </summary>
public record VerifyCodeRequest(
    string PhoneNumber,
    string Code,
    string? UserType = "Provider"
);

/// <summary>
/// Request model for generating token with additional claims
/// </summary>
public record GenerateTokenRequest
{
    public string UserId { get; init; } = string.Empty;
    public Dictionary<string, string>? AdditionalClaims { get; init; }
}

/// <summary>
/// Response model for token generation
/// </summary>
public record GenerateTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public string TokenType { get; init; } = "Bearer";
}
