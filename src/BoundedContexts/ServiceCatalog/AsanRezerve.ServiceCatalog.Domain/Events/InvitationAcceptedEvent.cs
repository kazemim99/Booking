using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record InvitationAcceptedEvent(
        Guid InvitationId,
        ProviderId OrganizationId,
        ProviderId IndividualProviderId,
        DateTime AcceptedAt) : DomainEvent;
}
