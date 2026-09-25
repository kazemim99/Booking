// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/ServiceDeactivatedEvent.cs
// ========================================


namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record ServiceDeactivatedEvent(
        ServiceId ServiceId,
        DateTime DeactivatedAt,
        string Reason) : DomainEvent;
}