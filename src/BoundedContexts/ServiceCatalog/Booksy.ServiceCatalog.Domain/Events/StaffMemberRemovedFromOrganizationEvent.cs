using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record StaffMemberRemovedFromOrganizationEvent(
        ProviderId OrganizationId,
        ProviderId IndividualProviderId,
        string Reason,
        DateTime RemovedAt) : DomainEvent;
}
