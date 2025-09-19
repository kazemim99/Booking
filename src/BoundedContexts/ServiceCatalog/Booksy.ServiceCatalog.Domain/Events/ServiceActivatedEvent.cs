// ========================================
// Booksy.ServiceCatalog.Domain/Events/ServiceActivatedEvent.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record ServiceActivatedEvent(
        ServiceId ServiceId,
        ProviderId ProviderId,
        string ServiceName,
        DateTime ActivatedAt) : DomainEvent;
}