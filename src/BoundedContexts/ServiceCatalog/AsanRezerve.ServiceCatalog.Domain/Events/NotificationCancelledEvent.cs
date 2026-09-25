// ========================================
// AsanRezerve.ServiceCatalog.Domain/Events/NotificationCancelledEvent.cs
// ========================================
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Events
{
    public sealed record NotificationCancelledEvent(
        NotificationId NotificationId,
        UserId RecipientId,
        NotificationType Type,
        string Reason) : DomainEvent;
}
