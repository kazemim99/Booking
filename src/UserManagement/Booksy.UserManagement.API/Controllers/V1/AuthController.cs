using Booksy.API.Extensions;
using Booksy.API.Middleware;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Booksy.UserManagement.Application.CQRS.Commands.CompleteProviderAuthentication;
using Booksy.UserManagement.Application.CQRS.Commands.SendVerificationCode;
using Booksy.UserManagement.Application.CQRS.Commands.CompleteCustomerAuthentication;

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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// Complete customer authentication (verify code + login/register)
    /// </summary>
    /// <param name="request">Customer authentication request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with JWT tokens</returns>
    /// <response code="200">Customer authenticated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Verification failed</response>
    [HttpPost("customer/complete-authentication")]
    [AllowAnonymous]
    [EnableRateLimiting("code-verification")]
    [ProducesResponseType(typeof(CompleteCustomerAuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompleteCustomerAuthentication(
        [FromBody] CompleteCustomerAuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var command = new CompleteCustomerAuthenticationCommand
            {
                PhoneNumber = request.PhoneNumber,
                Code = request.Code,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            var response = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "Customer authentication completed. UserId: {UserId}, CustomerId: {CustomerId}, IsNew: {IsNew}",
                response.UserId,
                response.CustomerId,
                response.IsNewCustomer
            );

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer authentication failed: {Message}", ex.Message);
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during customer authentication");
            return StatusCode(500, new { message = "An error occurred during authentication" });
        }
    }

    /// <summary>
    /// Complete provider authentication (verify code + login/register)
    /// </summary>
    /// <param name="request">Provider authentication request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with JWT tokens</returns>
    /// <response code="200">Provider authenticated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Verification failed</response>
    [HttpPost("provider/complete-authentication")]
    [AllowAnonymous]
    [EnableRateLimiting("code-verification")]
    [ProducesResponseType(typeof(CompleteProviderAuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompleteProviderAuthentication(
        [FromBody] CompleteProviderAuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var command = new CompleteProviderAuthenticationCommand
            {
                PhoneNumber = request.PhoneNumber,
                Code = request.Code,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            var response = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "Provider authentication completed. UserId: {UserId}, ProviderId: {ProviderId}, IsNew: {IsNew}",
                response.UserId,
                response.ProviderId,
                response.IsNewProvider
            );

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Provider authentication failed: {Message}", ex.Message);
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during provider authentication");
            return StatusCode(500, new { message = "An error occurred during authentication" });
        }
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
    //[HttpPost("generate-token")]
    //[ProducesResponseType(typeof(GenerateTokenResponse), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> GenerateToken(
    //    [FromBody] GenerateTokenRequest request,
    //    [FromServices] Application.Services.Interfaces.IJwtTokenService jwtTokenService,
    //    CancellationToken cancellationToken = default)
    //{
    //    // Validate user ID from request
    //    if (!Guid.TryParse(request.UserId, out var userId))
    //    {
    //        throw new DomainValidationException("UserId", "Invalid user ID format");
    //    }

    //    _logger.LogInformation(
    //        "Generating new token for user {UserId} with additional claims",
    //        userId);

    //    try
    //    {
    //        // Get user - use mediator or repository depending on your architecture
    //        var getUserQuery = new Application.CQRS.Queries.GetUserById.GetUserByIdQuery(userId);
    //        var user = await _mediator.Send(getUserQuery, cancellationToken);

    //        if (user == null)
    //        {
    //            _logger.LogWarning("User {UserId} not found for token generation", userId);
    //            throw new NotFoundException("User not found");
    //        }

    //        // Extract providerId and providerStatus from additional claims if present
    //        string? providerId = null;
    //        string? providerStatus = null;
    //        if (request.AdditionalClaims != null)
    //        {
    //            if (request.AdditionalClaims.TryGetValue("provider_id", out var providerIdValue))
    //            {
    //                providerId = providerIdValue;
    //            }
    //            if (request.AdditionalClaims.TryGetValue("provider_status", out var providerStatusValue))
    //            {
    //                providerStatus = providerStatusValue;
    //            }
    //        }

    //        // Parse user type from string
    //        var userType = Enum.TryParse<Domain.Enums.UserType>(user.Type, out var parsedType)
    //            ? parsedType
    //            : Domain.Enums.UserType.Customer;

    //        // Extract role names from RoleViewModel list
    //        var roleNames = user.Roles.Select(r => r.Name).ToList();

    //        // Generate new access token using the correct signature
    //        var accessToken = jwtTokenService.GenerateAccessToken(
    //            Core.Domain.ValueObjects.UserId.From(user.UserId),
    //            userType,
    //            Core.Domain.ValueObjects.Email.Create(user.Email ?? string.Empty),
    //            user.DisplayName ?? string.Empty,
    //            user.Status ?? "Active",
    //            roleNames,
    //            providerId,
    //            providerStatus,
    //            customerId:request.cu
    //            15 // 15 minutes expiration
    //        );

    //        // Generate refresh token (user aggregate handles this)
    //        var refreshTokenValue = Guid.NewGuid().ToString();

    //        _logger.LogInformation(
    //            "Successfully generated new token for user {UserId} with providerId: {ProviderId}",
    //            userId,
    //            providerId ?? "none");

    //        return Ok(new GenerateTokenResponse
    //        {
    //            AccessToken = accessToken,
    //            RefreshToken = refreshTokenValue,
    //            ExpiresIn = 900, // 15 minutes
    //            TokenType = "Bearer"
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error generating token for user {UserId}", userId);
    //        return StatusCode(500, new { message = "An error occurred while generating the token" });
    //    }
    //}
}

/// <summary>
/// Request model for sending verification code
/// </summary>
public record SendVerificationCodeRequest(
    string PhoneNumber,
    string CountryCode ="+98"
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

/// <summary>
/// Request model for customer authentication
/// </summary>
public record CompleteCustomerAuthenticationRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
}

/// <summary>
/// Request model for provider authentication
/// </summary>
public record CompleteProviderAuthenticationRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
}
