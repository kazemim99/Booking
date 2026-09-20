// ========================================
// Booksy.ServiceCatalog.Application/Queries/Notifications/GetDeliveryStatus/GetDeliveryStatusQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetDeliveryStatus
{
    /// <summary>
    /// Query to get delivery status of a specific notification
    /// </summary>
    /// <summary>
    /// The delivery record of one notification, for the person it was addressed to.
    /// </summary>
    /// <remarks>
    /// <paramref name="RequestedBy"/> is not a filter, it is the authorization: this endpoint previously took
    /// a notification id alone, so any signed-in user could read the delivery record — including the phone
    /// number and gateway response — of any notification whose id they could guess or observe.
    /// </remarks>
    public sealed record GetDeliveryStatusQuery(
        Guid NotificationId,
        Guid RequestedBy) : IQuery<DeliveryStatusViewModel?>;
}
