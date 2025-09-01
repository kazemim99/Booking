// ========================================
// Booksy.UserManagement.Domain/Exceptions/UserManagementDomainException.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Enums;

namespace Booksy.UserManagement.Domain.Repositories
{
    /// <summary>
    /// Read repository for User aggregate
    /// </summary>
    public interface IUserReadRepository : IReadRepository<User, UserId>
    {
        /// <summary>
        /// Gets a user by email
        /// </summary>
        Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user exists by email
        /// </summary>
        Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);

     
        /// <summary>
        /// Gets users by type
        /// </summary>
        Task<IReadOnlyList<User>> GetByTypeAsync(UserType type, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets users by role
        /// </summary>
        Task<IReadOnlyList<User>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets users registered between dates
        /// </summary>
        Task<IReadOnlyList<User>> GetRegisteredBetweenAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets paginated users with optional filters
        /// </summary>
        //Task<(IReadOnlyList<User> Users, int TotalCount)> GetPaginatedAsync(
        //    int pageNumber,
        //    int pageSize,
        //    UserStatus? status = null,
        //    UserType? type = null,
        //    CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches users by name or email
        /// </summary>
        Task<IQueryable<User>> SearchAsync(
            string searchTerm,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets users with expired activation tokens
        /// </summary>
        Task<IReadOnlyList<User>> GetUsersWithExpiredActivationTokensAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets locked accounts that can be unlocked
        /// </summary>
        Task<IReadOnlyList<User>> GetExpiredLockedAccountsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets user statistics
        /// </summary>
        Task<UserStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }

    public sealed class UserStatistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingUsers { get; set; }
        public int SuspendedUsers { get; set; }
        public int DeletedUsers { get; set; }
        public Dictionary<string, int> UsersByType { get; set; } = new();
        public int UsersRegisteredToday { get; set; }
        public int UsersRegisteredThisWeek { get; set; }
        public int UsersRegisteredThisMonth { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; }
    }
}