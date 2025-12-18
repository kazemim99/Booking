// ========================================
// Booksy.Core.Application/Abstractions/Persistence/IUnitOfWork.cs
// ========================================
using Microsoft.EntityFrameworkCore.Storage;

namespace Booksy.Core.Application.Abstractions.Persistence
{
    /// <summary>
    /// Defines a unit of work for managing transactions
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Commits all changes made in this unit of work
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of state entries written to the database</returns>
        Task<int> CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits all changes and publishes domain events (events dispatched BEFORE saving)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of state entries written to the database</returns>
        Task<int> CommitAndPublishEventsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves changes to database first, then publishes domain events (events dispatched AFTER saving)
        /// Use this when events trigger external actions (e.g., SMS, email) that reference the saved entity.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of state entries written to the database</returns>
        Task<int> SaveAndPublishEventsAsync(CancellationToken cancellationToken = default);

        Task PublishEventsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        Task ExecuteInTransactionAsyncAndPublishEvent(Func<Task> operation, CancellationToken cancellationToken = default);
        Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a function within a transaction
        /// </summary>
        //Task<T> ExecuteInTransactionAsync<T>(
        //    Func<Task<T>> operation,
        //    CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets whether a transaction is currently active
        /// </summary>
        bool HasActiveTransaction { get; }
    }
}
