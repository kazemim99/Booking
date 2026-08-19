// ========================================
// Booksy.ServiceCatalog.Application/IntegrationEvents/NotificationRetryRequestedIntegrationEvent.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;

namespace Booksy.ServiceCatalog.Application.IntegrationEvents
{
    /// <summary>
    /// A notification's send failed and is still within its retry budget: hand it to the durable outbox so the
    /// retry survives the process that produced the failure.
    /// </summary>
    /// <remarks>
    /// Published only on failure. The happy path never touches the bus — a delivered notification would just be
    /// de-duplicated on the other side, so the message would be pure overhead on every booking.
    /// </remarks>
    public sealed record NotificationRetryRequestedIntegrationEvent(
        Guid NotificationId,
        string Reason) : IntegrationEvent("ServiceCatalog");
}
