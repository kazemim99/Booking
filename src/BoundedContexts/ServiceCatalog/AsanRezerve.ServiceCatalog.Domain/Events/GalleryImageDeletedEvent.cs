// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/GalleryImageDeletedEvent.cs
// ========================================

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record GalleryImageDeletedEvent(
        ProviderId ProviderId,
        Guid ImageId,
        DateTime DeletedAt) : DomainEvent;
}
