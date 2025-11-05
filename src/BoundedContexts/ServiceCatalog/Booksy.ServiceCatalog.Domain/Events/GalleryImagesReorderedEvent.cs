// ========================================
// Booksy.ServiceCatalog.Domain/Events/GalleryImagesReorderedEvent.cs
// ========================================

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record GalleryImagesReorderedEvent(
        ProviderId ProviderId,
        Dictionary<Guid, int> ImageOrders,
        DateTime ReorderedAt) : DomainEvent;
}
