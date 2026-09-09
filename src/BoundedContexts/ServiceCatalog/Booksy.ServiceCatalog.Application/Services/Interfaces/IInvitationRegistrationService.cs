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
    /// Generates the current OTP code for a phone number (TOTP — deterministic for the same
    /// phone and time window, no record to persist). The caller is responsible for actually
    /// delivering it (SMS); this only computes the value <see cref="VerifyOtpAsync"/> will accept.
    /// </summary>
    Task<string> GenerateOtpCodeAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a user account for a verified phone number — or reuses the one that appeared
    /// for that phone between the caller's own lookup and this call. Returns the person id and
    /// whether this call created the account; only a created account may be compensated away.
    /// </summary>
    Task<CreatedPersonAccount> CreateUserWithPhoneAsync(
        string phoneNumber,
        string firstName,
        string lastName,
        string? email,
        CancellationToken cancellationToken = default);

    // CreateIndividualProviderAsync and GenerateAuthTokensAsync were removed with the
    // accept-with-registration saga. Joining a salon makes a person a MEMBER of it, so
    // there is no individual provider profile to create; and acceptance no longer mints a
    // session — the new member signs in with their own phone through the normal OTP flow.

    /// <summary>
    /// Compensation: Deletes a user account if registration flow fails
    /// Used for saga pattern rollback
    /// </summary>
    Task<bool> DeleteUserAsync(
        UserId userId,
        string reason,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of <see cref="IInvitationRegistrationService.CreateUserWithPhoneAsync"/>: the person,
/// and whether that call created the account (true) or found one that had appeared for the
/// phone in the meantime (false). Compensation must only ever delete a created account.
/// </summary>
public sealed record CreatedPersonAccount(UserId PersonId, bool IsNewAccount);
