//using Booksy.SharedKernel.Infrastructure.Persistence;
//using Booksy.SharedKernel.Infrastructure.EventBus;
//using Booksy.SharedKernel.Domain.Abstractions;

//namespace Booksy.SharedKernel.Infrastructure.Persistence;

///// <summary>
///// Simple in-memory unit of work that dispatches domain events
///// </summary>
//public class InMemoryUnitOfWork : IUnitOfWork
//{
//    private readonly DomainEventDispatcher _eventDispatcher;
//    private readonly List<IAggregateRoot> _trackedAggregates = new();

//    public InMemoryUnitOfWork(DomainEventDispatcher eventDispatcher)
//    {
//        _eventDispatcher = eventDispatcher;
//    }

//    public void TrackAggregate(IAggregateRoot aggregate)
//    {
//        if (!_trackedAggregates.Contains(aggregate))
//        {
//            _trackedAggregates.Add(aggregate);
//        }
//    }

//    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
//    {
//        // In a real implementation, this would save to database
//        // Here we just dispatch domain events

//        var aggregatesWithEvents = _trackedAggregates.Where(a => a.DomainEvents.Any()).ToList();

//        await _eventDispatcher.DispatchEventsAsync(aggregatesWithEvents, cancellationToken);

//        _trackedAggregates.Clear();

//        return aggregatesWithEvents.Count; // Simulated affected records
//    }
//}