using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Domain.Abstractions.Entities;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Aggregates;

namespace AsanRezerve.UserManagement.Domain.Repositories
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

        /// <summary>
        /// The user who holds this refresh token (any state — the caller decides whether it is
        /// still valid), loaded with roles, profile and refresh tokens. Replaces loading every
        /// user and scanning their tokens in memory on each refresh.
        /// </summary>
        Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<List<User>> GetAllAsync(CancellationToken cancellationToken);
    }
}