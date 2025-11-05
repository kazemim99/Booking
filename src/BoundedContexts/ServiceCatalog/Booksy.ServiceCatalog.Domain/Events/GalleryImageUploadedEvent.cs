// ========================================
// Booksy.ServiceCatalog.Domain/Events/GalleryImageUploadedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record GalleryImageUploadedEvent(
        ProviderId ProviderId,
        Guid ImageId,
        string ImageUrl,
        DateTime UploadedAt) : DomainEvent;
}
