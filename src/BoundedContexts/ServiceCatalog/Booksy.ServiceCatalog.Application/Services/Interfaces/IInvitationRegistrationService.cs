using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Services.Interfaces;

/// <summary>
/// Service for handling invitation acceptance with user and provider registration
/// </summary>
public interface IInvitationRegistrationService
{
    /// <summary>
    /// Verifies OTP code for phone number
    /// </summary>
    Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user account with verified phone number
    /// Returns the created user ID
    /// </summary>
    Task<UserId> CreateUserWithPhoneAsync(
        string phoneNumber,
        string firstName,
        string lastName,
        string? email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an individual provider profile for a staff member
    /// </summary>
    Task<Provider> CreateIndividualProviderAsync(
        UserId userId,
        string firstName,
        string lastName,
        string phoneNumber,
        string? email,
        ProviderId organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates JWT access and refresh tokens for authenticated user
    /// </summary>
    Task<(string AccessToken, string RefreshToken)> GenerateAuthTokensAsync(
        UserId userId,
        ProviderId providerId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensation: Deletes a user account if registration flow fails
    /// Used for saga pattern rollback
    /// </summary>
    Task<bool> DeleteUserAsync(
        UserId userId,
        string reason,
        CancellationToken cancellationToken = default);
}
