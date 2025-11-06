// ========================================
// Booksy.ServiceCatalog.Domain/Events/NotificationCreatedEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record NotificationCreatedEvent(
        NotificationId NotificationId,
        UserId RecipientId,
        NotificationType Type,
        NotificationChannel Channel,
        NotificationPriority Priority,
        DateTime? ScheduledFor) : DomainEvent;
}
