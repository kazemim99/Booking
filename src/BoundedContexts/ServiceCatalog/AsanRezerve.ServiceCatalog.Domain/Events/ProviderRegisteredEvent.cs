// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/ProviderRegisteredEvent.cs
// ========================================


namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record ProviderRegisteredEvent(
        ProviderId ProviderId,
        UserId OwnerId,
        string BusinessName,
        ServiceCategory PrimaryCategory,
        DateTime RegisteredAt) : DomainEvent;
}