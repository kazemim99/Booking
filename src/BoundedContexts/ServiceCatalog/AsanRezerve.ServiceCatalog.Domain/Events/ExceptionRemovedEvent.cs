// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/ExceptionRemovedEvent.cs
// ========================================

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record ExceptionRemovedEvent(
        ProviderId ProviderId,
        Guid ExceptionId,
        DateTime RemovedAt) : DomainEvent;
}
