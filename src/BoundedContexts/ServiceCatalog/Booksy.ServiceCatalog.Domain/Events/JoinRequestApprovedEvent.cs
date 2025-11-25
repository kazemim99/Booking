using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record JoinRequestApprovedEvent(
        Guid JoinRequestId,
        ProviderId OrganizationId,
        ProviderId RequesterId,
        DateTime ApprovedAt) : DomainEvent;
}
