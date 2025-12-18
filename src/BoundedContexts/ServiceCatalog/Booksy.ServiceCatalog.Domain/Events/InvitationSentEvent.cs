using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record InvitationSentEvent(
        Guid InvitationId,
        ProviderId OrganizationId,
        string PhoneNumber,
        DateTime SentAt) : DomainEvent;
}
