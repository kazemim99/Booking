using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    /// <summary>
    /// A review that was publicly visible no longer is — hidden by an administrator, or edited by its author and
    /// back in the queue.
    /// </summary>
    /// <remarks>
    /// For notifications and auditing only; see <see cref="ReviewPublishedEvent"/> for why the rating is not
    /// recomputed from here.
    /// </remarks>
    public sealed record ReviewUnpublishedEvent(
        Guid ReviewId,
        ProviderId ProviderId,
        UserId CustomerId,
        Guid BookingId) : DomainEvent;
}
