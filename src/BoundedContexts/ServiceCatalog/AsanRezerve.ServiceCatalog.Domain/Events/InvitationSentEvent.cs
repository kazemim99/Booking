using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record InvitationSentEvent(
        Guid InvitationId,
        ProviderId OrganizationId,
        string PhoneNumber,
        DateTime SentAt) : DomainEvent;
}
