using Booksy.UserManagement.Domain.Entities;

namespace Booksy.UserManagement.Domain.Repositories;

public interface IPhoneVerificationRepository
{
    Task<PhoneVerification?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<PhoneVerification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PhoneVerification> AddAsync(PhoneVerification verification, CancellationToken cancellationToken = default);

    Task UpdateAsync(PhoneVerification verification, CancellationToken cancellationToken = default);

    Task DeleteAsync(PhoneVerification verification, CancellationToken cancellationToken = default);

    Task<int> DeleteExpiredAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveVerificationAsync(string phoneNumber, CancellationToken cancellationToken = default);
}
