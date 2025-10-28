// ========================================
// Booksy.ServiceCatalog.Domain/Events/ExceptionAddedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record ExceptionAddedEvent(
        ProviderId ProviderId,
        Guid ExceptionId,
        DateOnly Date,
        TimeOnly? OpenTime,
        TimeOnly? CloseTime,
        string Reason,
        DateTime AddedAt) : DomainEvent;
}
