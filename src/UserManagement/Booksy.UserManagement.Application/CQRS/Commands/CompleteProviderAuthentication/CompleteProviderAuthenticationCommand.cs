// ========================================
// CompleteProviderAuthenticationCommand.cs
// Unified command for provider phone verification and authentication
// ========================================

// ========================================
// CompleteProviderAuthenticationCommand.cs
// Unified command for provider phone verification and authentication
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.CompleteProviderAuthentication;

/// <summary>
/// Complete provider authentication after phone verification
/// Handles both new registration and existing provider login
/// </summary>
public sealed record CompleteProviderAuthenticationCommand : ICommand<CompleteProviderAuthenticationResponse>
{
    /// <summary>
    /// Phone number to authenticate
    /// </summary>
    public string PhoneNumber { get; init; } = string.Empty;

    /// <summary>
    /// Verification code (OTP)
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Provider's first name (optional, for new registrations)
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Provider's last name (optional, for new registrations)
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Provider's email (optional)
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// IP address of the request (for security logging)
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// User agent of the request
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Idempotency key for preventing duplicate requests
    /// </summary>
    public Guid? IdempotencyKey { get; init; }
}

/// <summary>
/// Response returned after successful provider authentication
/// </summary>
public sealed record CompleteProviderAuthenticationResponse
{
    /// <summary>
    /// Whether this is a new provider registration
    /// </summary>
    public bool IsNewProvider { get; init; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Provider ID (if provider profile exists)
    /// </summary>
    public Guid? ProviderId { get; init; }

    /// <summary>
    /// Provider status (Pending, Active, Inactive, etc.)
    /// </summary>
    public string? ProviderStatus { get; init; }

    /// <summary>
    /// Provider's phone number
    /// </summary>
    public string PhoneNumber { get; init; } = string.Empty;

    /// <summary>
    /// Provider's email (if provided)
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Provider's full name
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Refresh token for token renewal
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Token type (Bearer)
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Whether provider needs to complete onboarding
    /// </summary>
    public bool RequiresOnboarding { get; init; }
}
