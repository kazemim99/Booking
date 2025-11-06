// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetDeliveryStatus/GetDeliveryStatusQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetDeliveryStatus
{
    /// <summary>
    /// Query to get delivery status of a specific notification
    /// </summary>
    public sealed record GetDeliveryStatusQuery(
        Guid NotificationId) : IQuery<DeliveryStatusViewModel?>;
}
