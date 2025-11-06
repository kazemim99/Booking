// ========================================
// Booksy.UserManagement.Domain/Repositories/IPhoneVerificationRepository.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates.PhoneVerificationAggregate;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.Repositories;

/// <summary>
/// Repository interface for PhoneVerification aggregate
/// </summary>
public interface IPhoneVerificationRepository
{
    /// <summary>
    /// Gets a verification by its unique identifier
    /// </summary>
    Task<PhoneVerification?> GetByIdAsync(VerificationId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active verification by phone number and purpose
    /// </summary>
    Task<PhoneVerification?> GetByPhoneAndPurposeAsync(
        string phoneNumber,
        VerificationPurpose purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent active verification by phone number
    /// </summary>
    Task<PhoneVerification?> GetByPhoneNumberAsync(
        PhoneNumber phoneNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent verifications for a phone number within a time period (for rate limiting)
    /// </summary>
    Task<List<PhoneVerification>> GetRecentVerificationsByPhoneAsync(
        string phoneNumber,
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all expired verifications for cleanup
    /// </summary>
    Task<List<PhoneVerification>> GetExpiredVerificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new verification
    /// </summary>
    Task AddAsync(PhoneVerification verification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing verification
    /// </summary>
    Task UpdateAsync(PhoneVerification verification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a verification
    /// </summary>
    Task DeleteAsync(PhoneVerification verification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an active verification exists for a phone number
    /// </summary>
    Task<bool> ExistsActiveVerificationAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
