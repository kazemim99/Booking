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
