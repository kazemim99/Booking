// ========================================
// Booksy.ServiceCatalog.Domain/Events/ServiceDeactivatedEvent.cs
// ========================================


namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record ServiceDeactivatedEvent(
        ServiceId ServiceId,
        DateTime DeactivatedAt,
        string Reason) : DomainEvent;
}