using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record StaffMemberRemovedFromOrganizationEvent(
        ProviderId OrganizationId,
        ProviderId IndividualProviderId,
        string Reason,
        DateTime RemovedAt) : DomainEvent;
}
