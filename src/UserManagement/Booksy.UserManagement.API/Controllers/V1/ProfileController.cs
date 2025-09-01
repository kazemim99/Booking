//// ========================================
//// Controllers/V1/UsersController.cs
//// ========================================
//using Booksy.Core.Application.DTOs;
//using Booksy.UserManagement.Application.Queries.GetUserById;
//using Booksy.UserManagement.Domain.ValueObjects;
//using MediatR;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace Booksy.UserManagement.API.Controllers.V1;


//[ApiController]
//[ApiVersion("1.0")]
//[Route("api/v{version:apiVersion}/profile")]
//[Authorize]
//[Produces("application/json")]
//public class ProfileController : ControllerBase
//{
//    private readonly IMediator _mediator;
//    private readonly ILogger<ProfileController> _logger;

//    public ProfileController(IMediator mediator, ILogger<ProfileController> logger)
//    {
//        _mediator = mediator;
//        _logger = logger;
//    }

//    /// <summary>
//    /// Get current user profile
//    /// </summary>
//    [HttpGet]
//    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
//    public async Task<IActionResult> GetProfile()
//    {
//        var userId = User.FindFirst("sub")?.Value;
//        if (string.IsNullOrEmpty(userId))
//        {
//            return Unauthorized();
//        }

//        var query = new GetUserByIdQuery(new UserId(Guid.Parse(userId)));
//        var result = await _mediator.Send(query);

//        if (result == null)
//        {
//            return NotFound();
//        }

//        var response = new UserProfileResponse
//        {
//            Id = result.Id,
//            Email = result.Email,
//            FirstName = result.FirstName,
//            LastName = result.LastName,
//            PhoneNumber = result.PhoneNumber,
//            UserType = result.UserType,
//            Status = result.Status,
//            ProfilePictureUrl = result.ProfilePictureUrl,
//            Bio = result.Bio,
//            RegisteredAt = result.RegisteredAt,
//            LastLoginAt = result.LastLoginAt,
//            TwoFactorEnabled = result.TwoFactorEnabled,
//            EmailVerified = result.EmailVerified,
//            Roles = result.Roles
//        };

//        return Ok(response);
//    }

//    /// <summary>
//    /// Update current user profile
//    /// </summary>
//    [HttpPut]
//    [ProducesResponseType(StatusCodes.Status200OK)]
//    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
//    {
//        var userId = User.FindFirst("sub")?.Value;
//        if (string.IsNullOrEmpty(userId))
//        {
//            return Unauthorized();
//        }

//        // Command implementation would go here
//        // var command = new UpdateProfileCommand(...);
//        // var result = await _mediator.Send(command);

//        return Ok(new { message = "Profile updated successfully" });
//    }

//    /// <summary>
//    /// Enable two-factor authentication
//    /// </summary>
//    [HttpPost("2fa/enable")]
//    [ProducesResponseType(typeof(TwoFactorSetupResponse), StatusCodes.Status200OK)]
//    public async Task<IActionResult> EnableTwoFactor()
//    {
//        var userId = User.FindFirst("sub")?.Value;
//        if (string.IsNullOrEmpty(userId))
//        {
//            return Unauthorized();
//        }

//        // Implementation would generate TOTP secret and QR code
//        var response = new TwoFactorSetupResponse
//        {
//            Secret = "JBSWY3DPEHPK3PXP", // Example secret
//            QrCodeUri = "otpauth://totp/Booksy:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=Booksy",
//            BackupCodes = new[] { "12345678", "87654321", "11223344" }
//        };

//        return Ok(response);
//    }

//    /// <summary>
//    /// Disable two-factor authentication
//    /// </summary>
//    [HttpPost("2fa/disable")]
//    [ProducesResponseType(StatusCodes.Status200OK)]
//    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request)
//    {
//        var userId = User.FindFirst("sub")?.Value;
//        if (string.IsNullOrEmpty(userId))
//        {
//            return Unauthorized();
//        }

//        // Verify password and disable 2FA
//        return Ok(new { message = "Two-factor authentication disabled" });
//    }
//}

