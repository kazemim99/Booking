using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Repositories;

namespace Booksy.ServiceCatalog.Application.Queries.Notifications.GetInbox
{
    /// <summary>The caller's unread count — the badge.</summary>
    public sealed record GetUnreadCountQuery(Guid RecipientId) : IQuery<int>;

    public sealed class GetUnreadCountQueryHandler : IQueryHandler<GetUnreadCountQuery, int>
    {
        private readonly INotificationReadRepository _notifications;

        public GetUnreadCountQueryHandler(INotificationReadRepository notifications)
        {
            _notifications = notifications;
        }

        public Task<int> Handle(GetUnreadCountQuery query, CancellationToken cancellationToken) =>
            _notifications.GetUnreadCountAsync(UserId.From(query.RecipientId), cancellationToken);
    }
}
