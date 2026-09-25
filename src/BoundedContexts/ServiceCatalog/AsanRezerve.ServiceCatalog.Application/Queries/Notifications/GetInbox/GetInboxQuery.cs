using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Notifications.GetInbox
{
    /// <summary>One row in a person's notification list.</summary>
    /// <param name="Id">The notification.</param>
    /// <param name="EventCode">
    /// Which notification this is. The client selects its icon and layout from this, so it never has to
    /// pattern-match translated copy.
    /// </param>
    /// <param name="Subject">Title, as written when it was sent.</param>
    /// <param name="Body">Message, as written when it was sent.</param>
    /// <param name="CreatedAt">When it was raised.</param>
    /// <param name="ReadAt">When the recipient first opened it; null while unread.</param>
    /// <param name="DestinationKind">What tapping it opens, recomputed for this read.</param>
    /// <param name="DestinationId">The entity to open.</param>
    /// <param name="IsActionable">
    /// False when the target is gone or no longer belongs to the reader. The row still displays — its text
    /// is history — but tapping it must not lead anywhere.
    /// </param>
    public sealed record InboxItem(
        Guid Id,
        NotificationEventCode? EventCode,
        string Subject,
        string Body,
        DateTime CreatedAt,
        DateTime? ReadAt,
        string DestinationKind,
        Guid? DestinationId,
        bool IsActionable);

    /// <summary>A page of the caller's notifications, newest first.</summary>
    public sealed record InboxPage(
        IReadOnlyList<InboxItem> Items,
        int TotalCount,
        int UnreadCount,
        int PageNumber,
        int PageSize);

    /// <summary>
    /// Reads the caller's own inbox.
    /// </summary>
    /// <remarks>
    /// The recipient is taken from the authenticated caller by the controller and never from the request, so
    /// there is no shape of this query that reads somebody else's notifications.
    /// </remarks>
    public sealed record GetInboxQuery(
        Guid RecipientId,
        int PageNumber = 1,
        int PageSize = 20,
        bool UnreadOnly = false) : IQuery<InboxPage>;
}
