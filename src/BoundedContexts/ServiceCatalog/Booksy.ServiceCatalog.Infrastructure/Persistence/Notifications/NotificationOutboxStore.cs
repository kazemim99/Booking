using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications
{
    /// <summary>
    /// Reads and writes outbox rows on behalf of the sweep.
    /// </summary>
    public interface INotificationOutboxStore
    {
        /// <summary>
        /// Takes up to <paramref name="batchSize"/> due rows, marking them claimed for the lease period.
        /// Rows another sweep already holds are left alone.
        /// </summary>
        Task<IReadOnlyList<NotificationOutboxEntry>> ClaimDueAsync(
            int batchSize,
            DateTime utcNow,
            CancellationToken cancellationToken = default);

        /// <summary>Marks a claimed row done. Terminal.</summary>
        Task MarkProcessedAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a failed attempt, returning the row to pending after a backoff or dead-lettering it once
        /// the attempt budget is spent.
        /// </summary>
        Task MarkFailedAsync(Guid id, string error, DateTime utcNow, CancellationToken cancellationToken = default);

        /// <summary>
        /// Withdraws rows not yet sent for a subject — the reminders of a booking that was cancelled.
        /// Rows already processed are left as they are: that notification has gone out and cannot be recalled.
        /// </summary>
        /// <param name="onlyCodes">
        /// When given, only these notifications are withdrawn. Null withdraws everything unsent about the
        /// subject, which is right when the subject itself is off — a cancelled appointment — and wrong when
        /// only one conversation about it has ended, as when a review arrives.
        /// </param>
        Task<int> CancelPendingForSubjectAsync(
            string subjectType,
            Guid subjectId,
            DateTime utcNow,
            IReadOnlyCollection<NotificationEventCode>? onlyCodes = null,
            CancellationToken cancellationToken = default);
    }

    /// <inheritdoc />
    public sealed class NotificationOutboxStore : INotificationOutboxStore
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<NotificationOutboxStore> _logger;

        public NotificationOutboxStore(ServiceCatalogDbContext context, ILogger<NotificationOutboxStore> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <remarks>
        /// <para>Raw SQL rather than load-then-save, because the claim has to be atomic against other sweeps
        /// and EF cannot express <c>FOR UPDATE SKIP LOCKED</c>. Reading rows and then updating them would let
        /// two sweeps select the same row and both send it.</para>
        ///
        /// <para><c>SKIP LOCKED</c> is what makes a second sweep useful rather than blocked: it steps over
        /// rows already being claimed instead of waiting behind them.</para>
        ///
        /// <para>The predicate mirrors <see cref="NotificationOutboxPolicy.IsClaimable"/> — pending and due,
        /// or claimed with an expired lease. The policy is the readable statement of the rule and is unit
        /// tested; this is the same rule expressed where the database can enforce it atomically. They must be
        /// changed together.</para>
        /// </remarks>
        public async Task<IReadOnlyList<NotificationOutboxEntry>> ClaimDueAsync(
            int batchSize,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            var claimedUntil = utcNow + NotificationOutboxPolicy.ClaimLease;

            var rows = await _context.NotificationOutbox
                .FromSqlRaw(
                    """
                    UPDATE "ServiceCatalog"."NotificationOutbox" AS o
                    SET "State" = {0}, "ClaimedUntil" = {1}, "UpdatedAt" = {2}
                    WHERE o."Id" IN (
                        SELECT c."Id"
                        FROM "ServiceCatalog"."NotificationOutbox" AS c
                        WHERE (c."State" = {3}
                               OR (c."State" = {0} AND c."ClaimedUntil" <= {2}))
                          AND (c."ScheduledFor" IS NULL OR c."ScheduledFor" <= {2})
                        ORDER BY c."ScheduledFor" NULLS FIRST, c."CreatedAt"
                        FOR UPDATE SKIP LOCKED
                        LIMIT {4}
                    )
                    RETURNING o.*
                    """,
                    NotificationOutboxState.Claimed,
                    claimedUntil,
                    utcNow,
                    NotificationOutboxState.Pending,
                    batchSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (rows.Count > 0)
                _logger.LogDebug("Claimed {Count} outbox rows until {ClaimedUntil:o}", rows.Count, claimedUntil);

            return rows;
        }

        public async Task MarkProcessedAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken = default)
        {
            await _context.NotificationOutbox
                .Where(e => e.Id == id)
                .ExecuteUpdateAsync(
                    set => set
                        .SetProperty(e => e.State, NotificationOutboxState.Processed)
                        .SetProperty(e => e.ClaimedUntil, (DateTime?)null)
                        .SetProperty(e => e.UpdatedAt, utcNow),
                    cancellationToken);
        }

        public async Task MarkFailedAsync(
            Guid id,
            string error,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            // Read the attempt count rather than assume it: the row may have been retried by another sweep
            // since this one claimed it, and deciding "is the budget spent" from a stale number would either
            // give up early or retry forever.
            var current = await _context.NotificationOutbox
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new { e.AttemptCount })
                .FirstOrDefaultAsync(cancellationToken);

            if (current is null)
            {
                _logger.LogWarning("Outbox row {OutboxId} vanished before its failure could be recorded", id);
                return;
            }

            var attempts = current.AttemptCount + 1;
            var outcome = NotificationOutboxPolicy.OnFailure(attempts, utcNow);
            var truncated = error.Length > 2000 ? error[..2000] : error;

            await _context.NotificationOutbox
                .Where(e => e.Id == id)
                .ExecuteUpdateAsync(
                    set => set
                        .SetProperty(e => e.State, outcome.State)
                        .SetProperty(e => e.AttemptCount, attempts)
                        .SetProperty(e => e.LastError, truncated)
                        .SetProperty(e => e.ScheduledFor, outcome.RetryAt)
                        .SetProperty(e => e.ClaimedUntil, (DateTime?)null)
                        .SetProperty(e => e.UpdatedAt, utcNow),
                    cancellationToken);

            if (outcome.State == NotificationOutboxState.DeadLettered)
            {
                _logger.LogError(
                    "Outbox row {OutboxId} dead-lettered after {Attempts} attempts: {Error}",
                    id, attempts, truncated);
            }
        }

        public async Task<int> CancelPendingForSubjectAsync(
            string subjectType,
            Guid subjectId,
            DateTime utcNow,
            IReadOnlyCollection<NotificationEventCode>? onlyCodes = null,
            CancellationToken cancellationToken = default)
        {
            // Claimed rows are cancelled too: a reminder being swept right now is for an appointment that is
            // no longer happening, and the sweep re-checks state before it sends.
            var rows = _context.NotificationOutbox
                .Where(e => e.SubjectType == subjectType
                            && e.SubjectId == subjectId
                            && (e.State == NotificationOutboxState.Pending
                                || e.State == NotificationOutboxState.Claimed));

            // An empty list would otherwise mean "withdraw nothing" while reading like "withdraw these",
            // so it is treated as the caller having nothing to withdraw rather than as no filter at all.
            if (onlyCodes is not null)
            {
                var codes = onlyCodes.ToList();
                if (codes.Count == 0)
                    return 0;

                rows = rows.Where(e => codes.Contains(e.EventCode));
            }

            var cancelled = await rows
                .ExecuteUpdateAsync(
                    set => set
                        .SetProperty(e => e.State, NotificationOutboxState.Cancelled)
                        .SetProperty(e => e.ClaimedUntil, (DateTime?)null)
                        .SetProperty(e => e.UpdatedAt, utcNow),
                    cancellationToken);

            if (cancelled > 0)
            {
                _logger.LogInformation(
                    "Cancelled {Count} unsent notification(s) for {SubjectType} {SubjectId}",
                    cancelled, subjectType, subjectId);
            }

            return cancelled;
        }
    }
}
