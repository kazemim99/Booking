using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;



namespace Booksy.Infrastructure.Core.Persistence.Base;

/// <summary>
/// Entity Framework Core implementation of Unit of Work pattern
/// </summary>
public class EfCoreUnitOfWork<TContext> : IUnitOfWork
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly ILogger<EfCoreUnitOfWork<TContext>> _logger;
    private IDbContextTransaction? _currentTransaction;

    public bool HasActiveTransaction => false;

    public EfCoreUnitOfWork(
        TContext context,
        ILogger<EfCoreUnitOfWork<TContext>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Dispatch domain events before saving
            //await DispatchDomainEventsAsync(cancellationToken);

            //// Update audit fields
            //UpdateAuditableEntities();

            // Save changes to database
            var result = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Saved {Count} changes to database", result);

            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency exception occurred");
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update exception occurred");
            throw;
        }
    }

    //public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    //{
    //    if (_currentTransaction != null)
    //    {
    //        throw new InvalidOperationException("Transaction already in progress");
    //    }

    //    _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    //    _logger.LogDebug("Transaction started");

    //    return _currentTransaction;
    //}

    //public async Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    //{
    //    if (transaction == null)
    //    {
    //        throw new ArgumentNullException(nameof(transaction));
    //    }

    //    if (transaction != _currentTransaction)
    //    {
    //        throw new InvalidOperationException("Transaction is not current");
    //    }

    //    try
    //    {
    //        await SaveChangesAsync(cancellationToken);
    //        await transaction.CommitAsync(cancellationToken);
    //        _logger.LogDebug("Transaction committed");
    //    }
    //    catch
    //    {
    //        await RollbackTransactionAsync(transaction, cancellationToken);
    //        throw;
    //    }
    //    finally
    //    {
    //        if (_currentTransaction != null)
    //        {
    //            _currentTransaction.Dispose();
    //            _currentTransaction = null;
    //        }
    //    }
    //}



    //public async Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    //{
    //    try
    //    {
    //        await transaction.RollbackAsync(cancellationToken);
    //        _logger.LogDebug("Transaction rolled back");
    //    }
    //    finally
    //    {
    //        if (_currentTransaction != null)
    //        {
    //            _currentTransaction.Dispose();
    //            _currentTransaction = null;
    //        }
    //    }
    //}

    //private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    //{
    //    var domainEntities = _context.ChangeTracker
    //        .Entries<IAggregateRoot>()
    //        .Where(x => x.Entity.DomainEvents?.Any() == true)
    //        .Select(x => x.Entity)
    //        .ToList();

    //    if (domainEntities.Any())
    //    {
    //        await _eventDispatcher.DispatchEventsAsync(domainEntities, cancellationToken);
    //    }
    //}

    //private void UpdateAuditableEntities()
    //{
    //    var entries = _context.ChangeTracker
    //        .Entries()
    //        .Where(e => e.Entity is IAuditableEntity &&
    //                   (e.State == EntityState.Added || e.State == EntityState.Modified));

    //    foreach (var entry in entries)
    //    {
    //        var entity = (IAuditableEntity)entry.Entity;

    //        if (entry.State == EntityState.Added)
    //        {
    //            entity.CreatedAt = DateTime.UtcNow;
    //            entity.CreatedBy = GetCurrentUserId();
    //        }

    //        entity.LastModifiedAt = DateTime.UtcNow;
    //        entity.LastModifiedBy = GetCurrentUserId();
    //    }
    //}

    protected virtual string? GetCurrentUserId() => "System";

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        var result = await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Saved {Count} changes to database", result);
        return result;

    }


    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if(_currentTransaction != null)
        await _currentTransaction.RollbackAsync();

    }


    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation();
                await SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation(); // <-- do all repository work here
                await SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context?.Dispose();
    }

    public async Task<int> CommitAndPublishEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {

            // Collect domain events before saving
            var aggregates = _context.ChangeTracker.Entries<IAggregateRoot>()
                .Where(x => x.Entity.DomainEvents?.Any() == true)
                .Select(x => x.Entity)
                .ToList();

            // Save changes to database first
            var result = await _context.SaveChangesAsync(cancellationToken);

            // Dispatch events only after successful save
            if (result > 0 && aggregates.Any())
            {
                //await _eventDispatcher.DispatchEventsAsync(aggregates, cancellationToken);

                // Clear events after successful dispatch
                foreach (var aggregate in aggregates)
                {
                    aggregate.ClearDomainEvents();
                }
            }

            _logger.LogDebug("Saved {Count} changes and dispatched events", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during commit and publish events");
            throw;
        }
    }
}
