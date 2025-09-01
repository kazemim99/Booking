using Booksy.Core.Domain.Infrastructure.Persistence;
using Booksy.Core.Domain.Abstractions;

namespace Booksy.Core.Domain.Domain.Exceptions;

/// <summary>
/// Simple in-memory repository for development and testing
/// </summary>
public class InMemoryRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot
    where TId : notnull
{
    private readonly Dictionary<TId, TAggregate> _store = new();
    private readonly Func<TAggregate, TId> _getKey;

    public InMemoryRepository(Func<TAggregate, TId> getKey)
    {
        _getKey = getKey;
    }

    public Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var aggregate);
        return Task.FromResult(aggregate);
    }

    public Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var id = _getKey(aggregate);
        _store[id] = aggregate;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var id = _getKey(aggregate);
        _store.Remove(id);
        return Task.CompletedTask;
    }

    // Additional helper methods
    public Task<IEnumerable<TAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<TAggregate>>(_store.Values);
    }

    public Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.ContainsKey(id));
    }

    public void Clear()
    {
        _store.Clear();
    }
}