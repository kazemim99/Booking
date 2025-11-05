// ========================================
// Booksy.ServiceCatalog.Domain/Events/GalleryImageDeletedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record GalleryImageDeletedEvent(
        ProviderId ProviderId,
        Guid ImageId,
        DateTime DeletedAt) : DomainEvent;
}
