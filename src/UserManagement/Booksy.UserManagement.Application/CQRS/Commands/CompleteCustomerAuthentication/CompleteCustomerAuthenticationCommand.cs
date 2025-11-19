// ========================================
// CompleteCustomerAuthenticationCommand.cs
// Unified command for customer phone verification and authentication
// ========================================

// ========================================
// CompleteCustomerAuthenticationCommand.cs
// Unified command for customer phone verification and authentication
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.UserManagement.Application.CQRS.Commands.CompleteCustomerAuthentication;

/// <summary>
/// Complete customer authentication after phone verification
/// Handles both new registration and existing customer login
/// </summary>
public sealed record CompleteCustomerAuthenticationCommand : ICommand<CompleteCustomerAuthenticationResponse>
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
    /// Customer's first name (optional, for new registrations)
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Customer's last name (optional, for new registrations)
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Customer's email (optional)
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
/// Response returned after successful customer authentication
/// </summary>
public sealed record CompleteCustomerAuthenticationResponse
{
    /// <summary>
    /// Whether this is a new customer registration
    /// </summary>
    public bool IsNewCustomer { get; init; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Customer ID
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Customer's phone number
    /// </summary>
    public string PhoneNumber { get; init; } = string.Empty;

    /// <summary>
    /// Customer's email (if provided)
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Customer's full name
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
}
