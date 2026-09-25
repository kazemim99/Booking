// ========================================
// AsanRezerve.UserManagement.Infrastructure/Persistence/EventSourcing/EventSourcedUserRepository.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Events;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Infrastructure.Core.Persistence.EventStore;

namespace AsanRezerve.UserManagement.Infrastructure.EventSourcing
{
    /// <summary>
    /// Event store for User aggregate
    /// </summary>
    public interface IUserEventStore
    {
        Task SaveEventsAsync(UserId aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion, CancellationToken cancellationToken = default);
        Task<IEnumerable<IDomainEvent>> GetEventsAsync(UserId aggregateId, CancellationToken cancellationToken = default);
        Task<IEnumerable<IDomainEvent>> GetEventsAsync(UserId aggregateId, int fromVersion, CancellationToken cancellationToken = default);
        Task<long> GetLastVersionAsync(UserId aggregateId, CancellationToken cancellationToken = default);
        Task<IEnumerable<UserId>> GetAllAggregateIdsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<StoredEvent>> GetEventHistoryAsync(UserId aggregateId, CancellationToken cancellationToken = default);
    }
}


