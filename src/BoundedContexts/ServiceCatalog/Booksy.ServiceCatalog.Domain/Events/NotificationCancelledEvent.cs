// ========================================
// Booksy.ServiceCatalog.Domain/Events/NotificationCancelledEvent.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    public sealed record NotificationCancelledEvent(
        NotificationId NotificationId,
        UserId RecipientId,
        NotificationType Type,
        string Reason) : DomainEvent;
}
