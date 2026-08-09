using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;

namespace Booksy.UserManagement.Domain.Repositories
{
    /// <summary>
    /// Domain repository - combines essential read and write operations for business logic
    /// </summary>
    public interface IUserRepository : IWriteRepository<User, UserId>, IReadRepository<User, UserId>
    {
        // ✅ Domain-specific methods
        Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
        Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// True when a non-deleted account already exists for this phone number.
        /// The phone is matched across every legacy stored form (see
        /// <see cref="PhoneNumber.EquivalentForms"/>); soft-deleted rows are excluded
        /// by the DbContext query filter so a deleted person's phone is reclaimable.
        /// This is the application-level half of the global phone-uniqueness rule
        /// (the other half is the partial unique index on the users table).
        /// </summary>
        Task<bool> ExistsByPhoneNumberAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);
        Task<List<User>> GetAllAsync(CancellationToken cancellationToken);
    }
}