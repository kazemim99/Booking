// ========================================
// Booksy.ServiceCatalog.Domain/Events/ExceptionRemovedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record ExceptionRemovedEvent(
        ProviderId ProviderId,
        Guid ExceptionId,
        DateTime RemovedAt) : DomainEvent;
}
