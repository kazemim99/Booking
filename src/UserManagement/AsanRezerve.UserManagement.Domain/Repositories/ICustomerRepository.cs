using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Aggregates.CustomerAggregate;

namespace AsanRezerve.UserManagement.Domain.Repositories
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
