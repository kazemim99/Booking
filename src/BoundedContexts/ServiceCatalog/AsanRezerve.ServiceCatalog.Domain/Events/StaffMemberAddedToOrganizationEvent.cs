using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record StaffMemberAddedToOrganizationEvent(
        ProviderId OrganizationId,
        ProviderId IndividualProviderId,
        DateTime AddedAt) : DomainEvent;
}
