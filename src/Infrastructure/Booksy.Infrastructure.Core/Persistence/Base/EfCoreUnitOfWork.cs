using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Abstractions.Events;
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
    private readonly IDomainEventDispatcher _eventDispatcher;
    private IDbContextTransaction? _currentTransaction;

    public bool HasActiveTransaction => _currentTransaction != null;

    public EfCoreUnitOfWork(
        TContext context,
        ILogger<EfCoreUnitOfWork<TContext>> logger,
        IDomainEventDispatcher eventDispatcher)
    {
        _context = context;
        _logger = logger;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Dispatch domain events before saving
            await DispatchDomainEventsAsync(cancellationToken);

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

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("Transaction already in progress");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogDebug("Transaction started");

        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (transaction != _currentTransaction)
        {
            throw new InvalidOperationException("Transaction is not current");
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Transaction committed");
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }


    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        // Get all tracked entities and filter for aggregates with domain events
        // We can't use Entries<IAggregateRoot>() because that only matches IAggregateRoot<Guid>
        // But our aggregates use strongly-typed IDs like IAggregateRoot<ProviderId>

        var allEntries = _context.ChangeTracker.Entries().ToList();
        _logger.LogDebug("Total tracked entities: {Count}", allEntries.Count);

        var allDomainEvents = new List<IDomainEvent>();
        var aggregatesWithEvents = new List<object>();

        foreach (var entry in allEntries)
        {
            var entity = entry.Entity;

            // Check if entity implements IAggregateRoot<T> for any T
            var aggregateInterface = entity.GetType()
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                                    i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));

            if (aggregateInterface != null)
            {
                // Get the DomainEvents property using reflection
                var domainEventsProperty = aggregateInterface.GetProperty("DomainEvents");
                if (domainEventsProperty != null)
                {
                    var domainEvents = domainEventsProperty.GetValue(entity) as System.Collections.IEnumerable;
                    if (domainEvents != null)
                    {
                        var eventsList = domainEvents.Cast<IDomainEvent>().ToList();
                        if (eventsList.Any())
                        {
                            allDomainEvents.AddRange(eventsList);
                            aggregatesWithEvents.Add(entity);

                            _logger.LogDebug("Found aggregate {Type} with {EventCount} domain events",
                                entity.GetType().Name, eventsList.Count);
                        }
                    }
                }
            }
        }

        if (allDomainEvents.Any())
        {
            _logger.LogInformation("Dispatching {EventCount} domain events from {AggregateCount} aggregates",
                allDomainEvents.Count, aggregatesWithEvents.Count);

            // Dispatch each event
            foreach (var domainEvent in allDomainEvents)
            {
                await _eventDispatcher.DispatchEventAsync(domainEvent, cancellationToken);
            }

            // Clear domain events from all aggregates using reflection
            foreach (var aggregate in aggregatesWithEvents)
            {
                var aggregateInterface = aggregate.GetType()
                    .GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                                        i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));

                if (aggregateInterface != null)
                {
                    var clearMethod = aggregateInterface.GetMethod("ClearDomainEvents");
                    clearMethod?.Invoke(aggregate, null);
                }
            }

            _logger.LogInformation("Domain events dispatched and cleared successfully");
        }
        else
        {
            _logger.LogDebug("No domain events to dispatch");
        }
    }

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

    protected virtual string? GetCurrentUserId() => "System";

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        // Delegate to SaveChangesAsync which dispatches events
        return await SaveChangesAsync(cancellationToken);
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
            // Check if transaction is already active
            if (_currentTransaction != null)
            {
                _logger.LogDebug("Transaction already active, reusing existing transaction");
                return await operation();
            }

            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation();
                await CommitAndPublishEventsAsync(cancellationToken);
                await _currentTransaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        });
    }

    public async Task ExecuteInTransactionAsyncAndPublishEvent(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            // Check if transaction is already active
            if (_currentTransaction != null)
            {
                _logger.LogDebug("Transaction already active, reusing existing transaction");
                await operation();
                return;
            }

            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation();
                await CommitAndPublishEventsAsync(cancellationToken);
                await _currentTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        });
    }
    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            // Check if transaction is already active
            if (_currentTransaction != null)
            {
                _logger.LogDebug("Transaction already active, reusing existing transaction");
                await operation();
                return;
            }

            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation();
                // IMPORTANT: Call SaveChangesAsync which dispatches domain events
                var result = await SaveChangesAsync(cancellationToken);
                await _currentTransaction.CommitAsync(cancellationToken);

                _logger.LogDebug("Saved {Count} changes and events", result);
            }
            catch
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
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
            // Dispatch domain events before saving
            await DispatchDomainEventsAsync(cancellationToken);

            // Save changes to database
            var result = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Saved {Count} changes and dispatched events", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during commit and publish events");
            throw;
        }
    }

    /// <summary>
    /// Saves changes to database first, then dispatches domain events.
    /// Use this when events trigger external actions (e.g., SMS, email) that reference the saved entity.
    /// </summary>
    public async Task<int> SaveAndPublishEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Save changes to database first
            var result = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Saved {Count} changes to database", result);

            // Then dispatch domain events
            await DispatchDomainEventsAsync(cancellationToken);

            _logger.LogDebug("Dispatched domain events after save");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during save and publish events");
            throw;
        }
    }
}
