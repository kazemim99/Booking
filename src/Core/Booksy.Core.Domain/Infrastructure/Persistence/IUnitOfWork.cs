namespace Booksy.Core.Domain.Infrastructure.Persistence;

/// <summary>
/// Unit of work pattern interface
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of affected records</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}