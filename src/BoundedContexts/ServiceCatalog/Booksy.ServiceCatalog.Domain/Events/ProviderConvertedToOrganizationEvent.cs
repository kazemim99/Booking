using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record ProviderConvertedToOrganizationEvent(
        ProviderId ProviderId,
        DateTime ConvertedAt) : DomainEvent;
}
