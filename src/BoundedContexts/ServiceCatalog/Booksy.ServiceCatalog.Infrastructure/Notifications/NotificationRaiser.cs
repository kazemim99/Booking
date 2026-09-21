using System.Text.Json;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Notifications
{
    /// <inheritdoc />
    /// <remarks>
    /// Adds the row to the caller's <see cref="ServiceCatalogDbContext"/> and stops. It deliberately does not
    /// save: the context is scoped, so the row is written by whatever commits the caller's work, which is how
    /// the intent and the business change end up in the same transaction.
    /// </remarks>
    public sealed class NotificationRaiser : INotificationRaiser
    {
        private static readonly JsonSerializerOptions ParameterJson = new(JsonSerializerDefaults.Web);

        private readonly ServiceCatalogDbContext _context;
        private readonly INotificationOutboxStore _outbox;
        private readonly ILogger<NotificationRaiser> _logger;

        public NotificationRaiser(
            ServiceCatalogDbContext context,
            INotificationOutboxStore outbox,
            ILogger<NotificationRaiser> logger)
        {
            _context = context;
            _outbox = outbox;
            _logger = logger;
        }

        public async Task RaiseAsync(
            NotificationEventCode code,
            Guid recipientId,
            Guid dedupKey,
            IReadOnlyDictionary<string, string>? parameters = null,
            string? subjectType = null,
            Guid? subjectId = null,
            DateTime? scheduledFor = null,
            CancellationToken cancellationToken = default)
        {
            if (code == NotificationEventCode.None)
                throw new ArgumentException("A notification must name which notification it is.", nameof(code));

            if (recipientId == Guid.Empty)
                throw new ArgumentException("A notification must have a recipient.", nameof(recipientId));

            if (dedupKey == Guid.Empty)
                throw new ArgumentException(
                    "A notification needs a de-duplication key — normally the id of the event that caused it.",
                    nameof(dedupKey));

            // Raising twice is a no-op rather than an error, because the callers are command handlers that may
            // legitimately run again (a retried command, a redelivered event) and the second run should not be
            // punished for it.
            //
            // This check is not the guarantee; the unique index is. Two raises racing in separate transactions
            // both pass this and one loses at commit. That is the correct outcome: they are the same logical
            // operation, and the loser's whole transaction was a duplicate.
            if (await AlreadyRaisedAsync(code, recipientId, dedupKey, cancellationToken))
            {
                _logger.LogDebug(
                    "Notification {Code} for {RecipientId} already raised under key {DedupKey}; skipping",
                    code, recipientId, dedupKey);
                return;
            }

            var now = DateTime.UtcNow;

            _context.NotificationOutbox.Add(new NotificationOutboxEntry
            {
                Id = Guid.NewGuid(),
                EventCode = code,
                RecipientId = recipientId,
                DedupKey = dedupKey,
                SubjectType = subjectType,
                SubjectId = subjectId,
                ParametersJson = Serialize(parameters),
                ScheduledFor = scheduledFor,
                State = NotificationOutboxState.Pending,
                AttemptCount = 0,
                CreatedAt = now,
                UpdatedAt = now,
            });

            _logger.LogDebug(
                "Raised {Code} for {RecipientId}{Scheduled}",
                code, recipientId,
                scheduledFor is null ? string.Empty : $" scheduled for {scheduledFor:o}");
        }

        public Task<int> WithdrawPendingForSubjectAsync(
            string subjectType,
            Guid subjectId,
            IReadOnlyCollection<NotificationEventCode>? onlyCodes = null,
            CancellationToken cancellationToken = default) =>
            _outbox.CancelPendingForSubjectAsync(
                subjectType, subjectId, DateTime.UtcNow, onlyCodes, cancellationToken);

        /// <summary>
        /// Looks in this unit of work as well as the table: two raises inside one command would both be
        /// pending in the change tracker and invisible to a database query.
        /// </summary>
        private async Task<bool> AlreadyRaisedAsync(
            NotificationEventCode code,
            Guid recipientId,
            Guid dedupKey,
            CancellationToken cancellationToken)
        {
            var pendingLocally = _context.NotificationOutbox.Local.Any(e =>
                e.DedupKey == dedupKey && e.EventCode == code && e.RecipientId == recipientId);

            if (pendingLocally)
                return true;

            return await _context.NotificationOutbox
                .AsNoTracking()
                .AnyAsync(
                    e => e.DedupKey == dedupKey && e.EventCode == code && e.RecipientId == recipientId,
                    cancellationToken);
        }

        private static string Serialize(IReadOnlyDictionary<string, string>? parameters) =>
            parameters is null || parameters.Count == 0
                ? "{}"
                : JsonSerializer.Serialize(parameters, ParameterJson);
    }
}
