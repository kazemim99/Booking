using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record InvitationAcceptedEvent(
        Guid InvitationId,
        ProviderId OrganizationId,
        ProviderId IndividualProviderId,
        DateTime AcceptedAt) : DomainEvent;
}
