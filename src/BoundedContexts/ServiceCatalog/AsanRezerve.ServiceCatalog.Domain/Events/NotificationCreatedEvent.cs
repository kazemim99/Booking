// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/NotificationCreatedEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record NotificationCreatedEvent(
        NotificationId NotificationId,
        UserId RecipientId,
        NotificationType Type,
        NotificationChannel Channel,
        NotificationPriority Priority,
        DateTime? ScheduledFor) : DomainEvent;
}
