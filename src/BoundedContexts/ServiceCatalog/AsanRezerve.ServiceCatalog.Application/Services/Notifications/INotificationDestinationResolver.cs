namespace AsanRezerve.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>A thing a notification can point at.</summary>
    /// <param name="Kind">"Booking", "Payment", "Provider", …</param>
    /// <param name="Id">Which one.</param>
    public readonly record struct NotificationTarget(string Kind, Guid Id);

    /// <summary>
    /// Works out, at read time, which of a page's notifications can still be opened.
    /// </summary>
    /// <remarks>
    /// <para><b>Why on read rather than on write.</b> A notification's words are history and never change. Its
    /// destination is not: a row saying "your booking is confirmed" must not still open a booking that has
    /// since been deleted, or one that now belongs to somebody else. Recomputing when the page is read keeps
    /// the text immutable, needs no fan-out write when a booking changes, and cannot be left stale by a missed
    /// event.</para>
    ///
    /// <para>Resolution is per page, in one batched lookup, rather than per row — an inbox page would
    /// otherwise be twenty round-trips.</para>
    /// </remarks>
    public interface INotificationDestinationResolver
    {
        /// <summary>
        /// Returns the subset of <paramref name="targets"/> that <paramref name="readerId"/> can still open.
        /// A target absent from the result is not actionable, whether because it is gone or because it is
        /// not theirs.
        /// </summary>
        Task<IReadOnlySet<NotificationTarget>> ResolveActionableAsync(
            Guid readerId,
            IReadOnlyCollection<NotificationTarget> targets,
            CancellationToken cancellationToken = default);
    }
}
