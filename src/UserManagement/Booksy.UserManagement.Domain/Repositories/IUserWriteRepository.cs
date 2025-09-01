// ========================================
// Booksy.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;

namespace Booksy.UserManagement.Domain.Repositories
{
    /// <summary>
    /// Write repository for User aggregate
    /// </summary>
    public interface IUserWriteRepository : IWriteRepository<User, UserId>
    {


        /// <summary>
        /// Gets a user by email
        /// </summary>
        Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);


        /// <summary>
        /// Adds a new user
        /// </summary>
        Task<User> AddUserAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing user
        /// </summary>
        Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a user (soft delete)
        /// </summary>
        Task DeleteUserAsync(UserId userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk update users
        /// </summary>
        Task BulkUpdateAsync(IEnumerable<User> users, CancellationToken cancellationToken = default);
    }
}


