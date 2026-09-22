using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
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
