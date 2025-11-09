// ========================================
// Booksy.UserManagement.Application/Commands/PhoneVerification/RegisterFromVerifiedPhone/RegisterFromVerifiedPhoneCommand.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Application.Commands.PhoneVerification.RegisterFromVerifiedPhone;

/// <summary>
/// Command to create a user account from a verified phone number
/// This is called after successful phone verification to create the user account
/// </summary>
public sealed record RegisterFromVerifiedPhoneCommand(
    string VerificationId,
    UserType UserType,
    string? FirstName = null,
    string? LastName = null,
    string? IpAddress = null,
    string? UserAgent = null
) : ICommand<RegisterFromVerifiedPhoneResult>;

/// <summary>
/// Result returned after creating user from verified phone
/// Includes authentication tokens for immediate login
/// </summary>
public sealed record RegisterFromVerifiedPhoneResult(
    Guid UserId,
    string PhoneNumber,
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string Message
);
