// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/GalleryImagesReorderedEvent.cs
// ========================================

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record GalleryImagesReorderedEvent(
        ProviderId ProviderId,
        Dictionary<Guid, int> ImageOrders,
        DateTime ReorderedAt) : DomainEvent;
}
