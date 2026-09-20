using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booksy.ServiceCatalog.Infrastructure.Notifications
{
    /// <inheritdoc />
    public sealed class NotificationDestinationResolver : INotificationDestinationResolver
    {
        private readonly ServiceCatalogDbContext _context;

        public NotificationDestinationResolver(ServiceCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlySet<NotificationTarget>> ResolveActionableAsync(
            Guid readerId,
            IReadOnlyCollection<NotificationTarget> targets,
            CancellationToken cancellationToken = default)
        {
            var actionable = new HashSet<NotificationTarget>();
            if (targets.Count == 0)
                return actionable;

            // One query per kind, not one per row. A page of twenty notifications about bookings is a single
            // lookup.
            await AddActionableBookingsAsync(readerId, targets, actionable, cancellationToken);
            AddPassThrough(targets, actionable, nameof(NotificationDestinationKind.Payment));
            AddPassThrough(targets, actionable, nameof(NotificationDestinationKind.Provider));

            return actionable;
        }

        /// <summary>
        /// A booking opens only if it still exists AND the reader is party to it — its customer, the salon
        /// that owns it, or the customer it was entered for.
        /// </summary>
        /// <remarks>
        /// Ownership is part of validity, not a separate permission check bolted on afterwards: a row whose
        /// booking now belongs to somebody else must not be openable by whoever was told about it once.
        /// </remarks>
        private async Task AddActionableBookingsAsync(
            Guid readerId,
            IReadOnlyCollection<NotificationTarget> targets,
            HashSet<NotificationTarget> actionable,
            CancellationToken cancellationToken)
        {
            // Compared as the strongly-typed id, not as its inner Guid: the value converter means
            // `b.Id.Value` has no SQL translation and the whole inbox query fails with a 500.
            var bookingIds = targets
                .Where(t => t.Kind == nameof(NotificationDestinationKind.Booking))
                .Select(t => BookingId.From(t.Id))
                .Distinct()
                .ToList();

            if (bookingIds.Count == 0)
                return;

            var readable = await _context.Bookings
                .AsNoTracking()
                .Where(b => bookingIds.Contains(b.Id))
                .Select(b => new { b.Id, b.CustomerId, b.ProviderId })
                .ToListAsync(cancellationToken);

            foreach (var booking in readable)
            {
                if (booking.CustomerId.Value == readerId || booking.ProviderId.Value == readerId)
                {
                    actionable.Add(new NotificationTarget(
                        nameof(NotificationDestinationKind.Booking), booking.Id.Value));
                }
            }
        }

        /// <summary>
        /// Kinds with no ownership check yet: the id is carried through as actionable.
        /// </summary>
        /// <remarks>
        /// Deliberately permissive rather than silently non-actionable — a payment or provider link that
        /// works today should keep working. Tightening these needs the same "is the reader party to it"
        /// question answered for each, which is its own piece of work; until then the screens behind them do
        /// their own authorization.
        /// </remarks>
        private static void AddPassThrough(
            IReadOnlyCollection<NotificationTarget> targets,
            HashSet<NotificationTarget> actionable,
            string kind)
        {
            foreach (var target in targets.Where(t => t.Kind == kind))
                actionable.Add(target);
        }
    }
}
