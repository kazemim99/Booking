// 📁 Booksy.UserManagement.Domain/Repositories/IUserRepository.cs - FIXED
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
        Task<List<User>> GetAllAsync(CancellationToken cancellationToken);
    }
}