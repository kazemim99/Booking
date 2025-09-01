using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.Abstractions.Entities;

namespace Booksy.Core.Application.Abstractions.Persistence;

/// <summary>
/// Generic repository interface
/// </summary>
/// <typeparam name="TAggregate">The aggregate type</typeparam>
/// <typeparam name="TId">The identifier type</typeparam>
public interface IRepository<TAggregate, in TId>
    where TAggregate : class, IAggregateRoot
    where TId : notnull
{
    /// <summary>
    /// Gets an aggregate by its identifier
    /// </summary>
    /// <param name="id">The identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregate if found, null otherwise</returns>
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an aggregate
    /// </summary>
    /// <param name="aggregate">The aggregate to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an aggregate
    /// </summary>
    /// <param name="aggregate">The aggregate to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}