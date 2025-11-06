// ========================================
// Booksy.UserManagement.API/Controllers/V1/PhoneVerificationController.cs
// ========================================
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Booksy.UserManagement.API.Models.Requests;
using Booksy.UserManagement.API.Models.Responses;
using Booksy.UserManagement.Application.Commands.PhoneVerification.RequestVerification;
using Booksy.UserManagement.Application.Commands.PhoneVerification.VerifyPhone;
using Booksy.UserManagement.Application.Commands.PhoneVerification.ResendOtp;
using Booksy.UserManagement.Domain.Enums;
using Booksy.Core.Domain.Exceptions;

namespace Booksy.UserManagement.API.Controllers.V1;

/// <summary>
/// Phone verification and OTP management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/phone-verification")]
[Produces("application/json")]
public class PhoneVerificationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PhoneVerificationController> _logger;

    public PhoneVerificationController(
        IMediator mediator,
        ILogger<PhoneVerificationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Request phone number verification - sends OTP code
    /// </summary>
    /// <param name="request">Phone verification request</param>
    /// <returns>Verification details including ID and expiration</returns>
    [HttpPost("request")]
    [AllowAnonymous]
    [EnableRateLimiting("phone-verification")]
    [ProducesResponseType(typeof(PhoneVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestVerification([FromBody] RequestPhoneVerificationRequest request)
    {
        // Get client metadata
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var sessionId = HttpContext.TraceIdentifier;

        // Parse enum values - throw validation exception on failure
        if (!Enum.TryParse<VerificationMethod>(request.Method, true, out var method))
        {
            throw new DomainValidationException("Method", $"Invalid verification method: {request.Method}");
        }

        if (!Enum.TryParse<VerificationPurpose>(request.Purpose, true, out var purpose))
        {
            throw new DomainValidationException("Purpose", $"Invalid verification purpose: {request.Purpose}");
        }

        var command = new RequestPhoneVerificationCommand(
            request.PhoneNumber,
            method,
            purpose,
            request.UserId,
            ipAddress,
            userAgent,
            sessionId);

        // Handler returns result directly or throws exception
        var result = await _mediator.Send(command);

        var response = new PhoneVerificationResponse
        {
            VerificationId = result.VerificationId,
            PhoneNumber = MaskPhoneNumber(result.PhoneNumber),
            Method = result.Method.ToString(),
            ExpiresAt = result.ExpiresAt,
            MaxAttempts = result.MaxAttempts,
            Message = result.Message
        };

        _logger.LogInformation(
            "Phone verification requested: VerificationId={VerificationId}, Phone={Phone}",
            response.VerificationId,
            result.PhoneNumber);

        return Ok(response);
    }

    /// <summary>
    /// Verify phone number with OTP code
    /// </summary>
    /// <param name="request">Verification request with code</param>
    /// <returns>Verification result</returns>
    [HttpPost("verify")]
    [AllowAnonymous]
    [EnableRateLimiting("phone-verification")]
    [ProducesResponseType(typeof(VerifyPhoneResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new VerifyPhoneCommand(
            request.VerificationId,
            request.Code,
            ipAddress);

        // Handler returns VerifyPhoneResult directly (has Success & Message properties)
        var result = await _mediator.Send(command);

        var response = new VerifyPhoneResponse
        {
            Success = result.Success,
            Message = result.Message,
            PhoneNumber = result.Success ? result.PhoneNumber : null,
            VerifiedAt = result.VerifiedAt,
            RemainingAttempts = result.RemainingAttempts,
            BlockedUntil = result.BlockedUntil
        };

        if (result.Success)
        {
            _logger.LogInformation(
                "Phone verified successfully: VerificationId={VerificationId}",
                request.VerificationId);
        }
        else
        {
            _logger.LogWarning(
                "Phone verification failed: VerificationId={VerificationId}, Remaining={Remaining}",
                request.VerificationId,
                result.RemainingAttempts);
        }

        return Ok(response);
    }

    /// <summary>
    /// Resend OTP code for existing verification
    /// </summary>
    /// <param name="request">Resend request with verification ID</param>
    /// <returns>Resend result</returns>
    [HttpPost("resend")]
    [AllowAnonymous]
    [EnableRateLimiting("phone-verification")]
    [ProducesResponseType(typeof(ResendOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var sessionId = HttpContext.TraceIdentifier;

        var command = new ResendOtpCommand(
            request.VerificationId,
            ipAddress,
            userAgent,
            sessionId);

        // Handler returns ResendOtpResult directly (has Success & Message properties)
        var result = await _mediator.Send(command);

        var response = new ResendOtpResponse
        {
            Success = result.Success,
            Message = result.Message,
            PhoneNumber = MaskPhoneNumber(result.PhoneNumber),
            ExpiresAt = result.ExpiresAt,
            RemainingResendAttempts = result.RemainingResendAttempts,
            CanResendAfter = result.CanResendAfter
        };

        if (result.Success)
        {
            _logger.LogInformation(
                "OTP resent: VerificationId={VerificationId}",
                request.VerificationId);
        }

        return Ok(response);
    }

    /// <summary>
    /// Masks phone number for security (shows only last 4 digits)
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return phoneNumber;

        var visibleDigits = 4;
        var maskedPart = new string('*', phoneNumber.Length - visibleDigits);
        var visiblePart = phoneNumber.Substring(phoneNumber.Length - visibleDigits);

        return maskedPart + visiblePart;
    }
}
