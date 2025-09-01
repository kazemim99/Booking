// ========================================
// Booksy.Core.Application/Abstractions/Persistence/IUnitOfWork.cs
// ========================================
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
        /// Commits all changes and publishes domain events
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of state entries written to the database</returns>
        Task<int> CommitAndPublishEventsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back all changes made in this unit of work
        /// </summary>
        //Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a new transaction
        /// </summary>
        //Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        ///// <summary>
        ///// Commits the current transaction
        ///// </summary>
        //Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

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
