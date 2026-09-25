using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Notifications
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
        /// A booking opens only if it still exists AND the reader is party to it — its customer, or someone who acts
        /// for the salon that owns it (its owner, or an active member).
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

            // The salon side reads with a USER id, while a booking belongs to the ORGANISATION: comparing the two
            // never matched, so no salon notification was ever tappable (QA 2026-09-22). The reader acts for a salon
            // it owns or holds an active membership in — the same rule as managing its bookings.
            var salonIds = readable.Select(b => b.ProviderId).Distinct().ToList();
            var reader = UserId.From(readerId);
            var ownedSalons = await _context.Providers
                .AsNoTracking()
                .Where(p => salonIds.Contains(p.Id) && p.OwnerId == reader)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
            var memberSalons = await _context.OrganizationMemberships
                .AsNoTracking()
                .Where(m => salonIds.Contains(m.OrganizationId)
                            && m.PersonId == reader
                            && m.Status != MembershipStatus.Terminated)
                .Select(m => m.OrganizationId)
                .ToListAsync(cancellationToken);
            var actsFor = ownedSalons.Concat(memberSalons).ToHashSet();

            foreach (var booking in readable)
            {
                if (booking.CustomerId.Value == readerId || actsFor.Contains(booking.ProviderId))
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
