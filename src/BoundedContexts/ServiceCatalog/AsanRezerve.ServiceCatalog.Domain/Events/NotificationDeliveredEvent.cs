// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/NotificationDeliveredEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record NotificationDeliveredEvent(
        NotificationId NotificationId,
        UserId RecipientId,
        NotificationType Type,
        NotificationChannel Channel,
        DateTime DeliveredAt) : DomainEvent;
}
