// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/NotificationSentEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record NotificationSentEvent(
        NotificationId NotificationId,
        UserId RecipientId,
        NotificationType Type,
        NotificationChannel Channel,
        int AttemptCount) : DomainEvent;
}
