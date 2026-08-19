// ========================================
// Booksy.ServiceCatalog.Infrastructure/EventHandlers/NotificationRetrySubscriber.cs
// ========================================
using Booksy.ServiceCatalog.Application.IntegrationEvents;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.EventHandlers;

/// <summary>
/// Retries notifications whose send failed, off the request path and out of the durable CAP outbox.
/// </summary>
/// <remarks>
/// <para>The outbox is what makes the retry survive the request that produced the failure — the message is
/// committed to Postgres before it is dispatched, so a process restart cannot lose it. CAP redelivers a handler
/// that throws, so a still-failing send simply throws again and is retried on CAP's schedule; the de-duplication
/// tuple in <c>NotificationDeliveries</c> is what makes those redeliveries safe.</para>
/// <para>Once the notification's retry budget is exhausted the dispatcher dead-letters it, and this handler
/// returns normally — throwing then would have CAP retrying a notification that is deliberately terminal.</para>
/// </remarks>
public sealed class NotificationRetrySubscriber : ICapSubscribe
{
    private readonly INotificationWriteRepository _notifications;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<NotificationRetrySubscriber> _logger;

    public NotificationRetrySubscriber(
        INotificationWriteRepository notifications,
        INotificationDispatcher dispatcher,
        ILogger<NotificationRetrySubscriber> logger)
    {
        _notifications = notifications;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    [CapSubscribe("booksy.servicecatalog.notificationretryrequested")]
    public async Task HandleAsync(NotificationRetryRequestedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        var notification = await _notifications.GetByIdAsync(NotificationId.From(@event.NotificationId), cancellationToken);

        if (notification is null)
        {
            _logger.LogWarning(
                "Retry requested for unknown notification {NotificationId}", @event.NotificationId);
            return;
        }

        var outcome = await _dispatcher.DispatchAsync(notification, cancellationToken);
        await _notifications.UpdateNotificationAsync(notification, cancellationToken);

        if (notification.Status is NotificationStatus.Delivered or NotificationStatus.Sent)
        {
            _logger.LogInformation(
                "Notification {NotificationId} delivered on retry after: {Reason}",
                @event.NotificationId, @event.Reason);
            return;
        }

        if (notification.Status == NotificationStatus.DeadLettered)
        {
            _logger.LogError(
                "Notification {NotificationId} dead-lettered; no further retries",
                @event.NotificationId);
            return;
        }

        if (outcome.AnyFailed)
        {
            // Surface the failure to CAP so the message is redelivered on its retry schedule.
            throw new InvalidOperationException(
                $"Notification {@event.NotificationId} still failing: {outcome.FirstError}");
        }
    }
}
