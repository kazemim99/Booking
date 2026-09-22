using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// A review became publicly visible — approved from the queue, or restored after being hidden.
    /// </summary>
    /// <param name="IsRepublication">
    /// True when this review had been public before: approved again after its author edited it, or restored.
    /// A provider must be able to tell a changed review from a new one.
    /// </param>
    /// <remarks>
    /// For notifications and auditing only. The provider's rating is NOT recomputed from this event: the unit of
    /// work dispatches events before it saves, on a fresh scope, so a handler would read the row unwritten.
    /// </remarks>
    public sealed record ReviewPublishedEvent(
        Guid ReviewId,
        ProviderId ProviderId,
        UserId CustomerId,
        Guid BookingId,
        bool IsRepublication) : DomainEvent;
}
