using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record ProviderConvertedToOrganizationEvent(
        ProviderId ProviderId,
        DateTime ConvertedAt) : DomainEvent;
}
