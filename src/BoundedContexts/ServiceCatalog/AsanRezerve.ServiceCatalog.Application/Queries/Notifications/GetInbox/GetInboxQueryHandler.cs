using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.Repositories;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Notifications.GetInbox
{
    /// <summary>
    /// Builds a page of the caller's inbox, recomputing each row's tap destination against current state.
    /// </summary>
    public sealed class GetInboxQueryHandler : IQueryHandler<GetInboxQuery, InboxPage>
    {
        private readonly INotificationReadRepository _notifications;
        private readonly INotificationDestinationResolver _destinations;

        public GetInboxQueryHandler(
            INotificationReadRepository notifications,
            INotificationDestinationResolver destinations)
        {
            _notifications = notifications;
            _destinations = destinations;
        }

        public async Task<InboxPage> Handle(GetInboxQuery query, CancellationToken cancellationToken)
        {
            var (notifications, totalCount) = await _notifications.GetUserNotificationHistoryAsync(
                UserId.From(query.RecipientId),
                channel: null,
                type: null,
                status: null,
                startDate: null,
                endDate: null,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize,
                cancellationToken);

            // Only what actually reached this person. The history query returns every row for a recipient,
            // including ones still Queued for a future time and ones that failed — putting either in an
            // inbox shows somebody a message no channel has delivered. This is also what keeps the list and
            // the unread badge in agreement, since the count is filtered the same way.
            var delivered = notifications
                .Where(n => n.Status is NotificationStatus.Sent
                                     or NotificationStatus.Delivered
                                     or NotificationStatus.Read)
                .Where(n => !IsLegacyHtml(n.EventCode, n.Body))
                .ToList();

            var visible = query.UnreadOnly
                ? delivered.Where(n => n.ReadAt is null).ToList()
                : delivered;

            var targets = visible
                .Select(TargetOf)
                .Where(t => t is not null)
                .Select(t => t!.Value)
                .Distinct()
                .ToList();

            // One lookup for the page. A row whose target is gone still renders — its words are history —
            // but it comes back non-actionable so a tap cannot lead nowhere.
            var actionable = targets.Count == 0
                ? new HashSet<NotificationTarget>()
                : (ISet<NotificationTarget>)await _destinations.ResolveActionableAsync(
                    query.RecipientId, targets, cancellationToken);

            var items = visible.Select(n =>
            {
                var target = TargetOf(n);
                return new InboxItem(
                    n.Id.Value,
                    n.EventCode,
                    n.Subject,
                    n.Body,
                    n.CreatedAt,
                    n.ReadAt,
                    target?.Kind ?? NotificationDestinationKind.None.ToString(),
                    target?.Id,
                    target is not null && actionable.Contains(target.Value));
            }).ToList();

            var unreadCount = await _notifications.GetUnreadCountAsync(
                UserId.From(query.RecipientId), cancellationToken);

            return new InboxPage(items, totalCount, unreadCount, query.PageNumber, query.PageSize);
        }

        /// <summary>
        /// What a notification points at, taken from the catalogue's destination kind and the related entity
        /// the notification already carries.
        /// </summary>
        /// <summary>
        /// A row written by the English HTML handlers deleted in f9502127 ("<h2>Your booking has been cancelled</h2>"):
        /// no event code, markup for a body. Kept in the database, never shown — nothing current writes HTML, and
        /// the unread count applies the same rule (QA walkthrough 2026-09-22).
        /// </summary>
        public static bool IsLegacyHtml(NotificationEventCode? eventCode, string? body) =>
            eventCode is null && body is not null && body.Contains('<');

        private static NotificationTarget? TargetOf(Notification notification)
        {
            if (notification.EventCode is not { } code
                || !NotificationEventCatalog.TryDescribe(code, out var descriptor))
            {
                return null;
            }

            return descriptor!.Destination switch
            {
                NotificationDestinationKind.Booking when notification.BookingId is { } b =>
                    new NotificationTarget(nameof(NotificationDestinationKind.Booking), b.Value),
                NotificationDestinationKind.Payment when notification.PaymentId is { } p =>
                    new NotificationTarget(nameof(NotificationDestinationKind.Payment), p.Value),
                NotificationDestinationKind.Provider when notification.ProviderId is { } pr =>
                    new NotificationTarget(nameof(NotificationDestinationKind.Provider), pr.Value),
                _ => null,
            };
        }
    }
}
