using Booksy.Core.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booksy.Core.Domain.Infrastructure.EventBus
{
    /// <summary>
    /// Interface for dispatching domain events from aggregates
    /// </summary>
    public interface IDomainEventDispatcher
    {
        /// <summary>
        /// Dispatches all domain events from an aggregate and clears them
        /// </summary>
        Task DispatchEventsAsync(IAggregateRoot aggregate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches events from multiple aggregates
        /// </summary>
        Task DispatchEventsAsync(IEnumerable<IAggregateRoot> aggregates, CancellationToken cancellationToken = default);
    }
}
