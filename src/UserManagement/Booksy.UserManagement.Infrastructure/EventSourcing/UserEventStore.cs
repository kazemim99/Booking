// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/EventSourcing/EventSourcedUserRepository.cs
// ========================================
using Microsoft.Extensions.Logging;
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Booksy.UserManagement.Infrastructure.Persistence.Configurations;
using Booksy.Infrastructure.Core.Persistence.EventStore;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Infrastructure.EventSourcing
{
    public class UserEventStore : IUserEventStore
    {
        private readonly UserManagementDbContext _context;
        private readonly ILogger<UserEventStore> _logger;

        public UserEventStore(UserManagementDbContext context, ILogger<UserEventStore> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SaveEventsAsync(
            UserId aggregateId,
            IEnumerable<IDomainEvent> events,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            var eventsList = events.ToList();
            if (!eventsList.Any())
                return;

            var currentVersion = await GetLastVersionAsync(aggregateId, cancellationToken);

            if (currentVersion != expectedVersion)
            {
                throw new InvalidOperationException($"Concurrency conflict. Expected version {expectedVersion} but was {currentVersion}");
            }

            var storedEvents = new List<StoredEvent>();
            var version = currentVersion;

            foreach (var domainEvent in eventsList)
            {
                version++;
                var storedEvent = new StoredEvent
                (
                     Guid.NewGuid().ToString(),
                    aggregateId.Value.ToString(),
                    "User",
                    domainEvent.GetType().Name,
                    JsonSerializer.Serialize(domainEvent),
                    version,
                    domainEvent.OccurredAt
                );

                storedEvents.Add(storedEvent);
            }

            await _context.Set<StoredEvent>().AddRangeAsync(storedEvents, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Saved {EventCount} events for User {AggregateId} with versions {FromVersion} to {ToVersion}",
                eventsList.Count,
                aggregateId,
                currentVersion + 1,
                version);
        }

        public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(
            UserId aggregateId,
            CancellationToken cancellationToken = default)
        {
            return await GetEventsAsync(aggregateId, 0, cancellationToken);
        }

        public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(
            UserId aggregateId,
            int fromVersion,
            CancellationToken cancellationToken = default)
        {
            var storedEvents = await _context.Set<StoredEvent>()
                .Where(e => e.AggregateId == aggregateId.Value.ToString() && e.Version > fromVersion)
                .OrderBy(e => e.Version)
                .ToListAsync(cancellationToken);

            var domainEvents = new List<IDomainEvent>();

            foreach (var storedEvent in storedEvents)
            {
                var eventType = Type.GetType($"Booksy.UserManagement.Domain.Events.{storedEvent.EventType}");
                if (eventType == null)
                {
                    _logger.LogWarning("Unknown event type: {EventType}", storedEvent.EventType);
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(storedEvent.EventData, eventType) as IDomainEvent;
                if (domainEvent != null)
                {
                    domainEvents.Add(domainEvent);
                }
            }

            return domainEvents;
        }

        public async Task<long> GetLastVersionAsync(
            UserId aggregateId,
            CancellationToken cancellationToken = default)
        {
            var lastEvent = await _context.Set<StoredEvent>()
                .Where(e => e.AggregateId == aggregateId.Value.ToString())
                .OrderByDescending(e => e.Version)
                .FirstOrDefaultAsync(cancellationToken);

            return lastEvent?.Version ?? 0;
        }

        public async Task<IEnumerable<UserId>> GetAllAggregateIdsAsync(
            CancellationToken cancellationToken = default)
        {
            var aggregateIds = await _context.Set<StoredEvent>()
                .Where(e => e.AggregateType == "User")
                .Select(e => e.AggregateId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return aggregateIds
                .Select(id => UserId.From(Guid.Parse(id)))
                .ToList();
        }

        public async Task<IEnumerable<StoredEvent>> GetEventHistoryAsync(UserId aggregateId, CancellationToken cancellationToken)
        {
            return await _context.Set<StoredEvent>()
                .Where(e => e.AggregateId == aggregateId.Value.ToString())
                .OrderBy(e => e.Version)
                .ToListAsync(cancellationToken);
        }




    }
}


