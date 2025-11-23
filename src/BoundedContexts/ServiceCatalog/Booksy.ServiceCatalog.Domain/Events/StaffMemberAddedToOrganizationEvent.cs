using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record StaffMemberAddedToOrganizationEvent(
        ProviderId OrganizationId,
        ProviderId IndividualProviderId,
        DateTime AddedAt) : DomainEvent;
}
