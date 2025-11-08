using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates.CustomerAggregate;

namespace Booksy.UserManagement.Domain.Repositories
{
    /// <summary>
    /// Domain repository - combines essential read and write operations for Customer business logic
    /// </summary>
    public interface ICustomerRepository : IWriteRepository<Customer, CustomerId>, IReadRepository<Customer, CustomerId>
    {
        // Domain-specific methods
        Task<Customer?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Customer>> GetByFavoriteProviderAsync(Guid providerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> GetFavoriteProviderIdsAsync(CustomerId customerId, CancellationToken cancellationToken = default);
        Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
