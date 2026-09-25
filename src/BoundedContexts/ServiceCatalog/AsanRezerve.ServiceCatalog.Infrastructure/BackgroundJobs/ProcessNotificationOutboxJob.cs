using System.Text.Json;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Identity;
using AsanRezerve.ServiceCatalog.Application.Commands.Notifications.SendNotification;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Turns recorded notification intents into sent notifications.
    /// </summary>
    /// <remarks>
    /// <para>This is the half of the outbox that runs after the business transaction has committed. It claims
    /// a batch of due rows, resolves each against the catalogue, and hands it to the existing send path, which
    /// already owns preference gating, de-duplication, retry and the delivery log. Nothing about how a
    /// notification is delivered is decided here.</para>
    ///
    /// <para>One row failing must not stop the batch: a single recipient with no phone number, or one gateway
    /// rejection, would otherwise hold up every other notification behind it.</para>
    /// </remarks>
    public sealed class ProcessNotificationOutboxJob
    {
        /// <summary>
        /// Rows per sweep. Small enough that a batch finishes well inside
        /// <see cref="NotificationOutboxPolicy.ClaimLease"/>, so a claim never expires under a sweep that is
        /// still working on it.
        /// </summary>
        private const int BatchSize = 50;

        private readonly INotificationOutboxStore _outbox;
        private readonly IPersonDirectory _people;
        private readonly INotificationCopyWriter _copy;
        private readonly ISender _mediator;
        private readonly ILogger<ProcessNotificationOutboxJob> _logger;

        public ProcessNotificationOutboxJob(
            INotificationOutboxStore outbox,
            IPersonDirectory people,
            INotificationCopyWriter copy,
            ISender mediator,
            ILogger<ProcessNotificationOutboxJob> logger)
        {
            _outbox = outbox;
            _people = people;
            _copy = copy;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var claimed = await _outbox.ClaimDueAsync(BatchSize, now, cancellationToken);
            if (claimed.Count == 0)
                return 0;

            // One lookup for the batch rather than one per row: a morning's reminders are otherwise a few
            // hundred round-trips for the same handful of people.
            var recipients = await _people.FindByIdsAsync(
                claimed.Select(r => r.RecipientId).Distinct().ToList(),
                cancellationToken);

            var sent = 0;

            foreach (var row in claimed)
            {
                try
                {
                    await ProcessAsync(row, recipients, cancellationToken);
                    await _outbox.MarkProcessedAsync(row.Id, DateTime.UtcNow, cancellationToken);
                    sent++;
                }
                catch (Exception ex)
                {
                    // Deliberately broad: whatever went wrong with this row, the rest of the batch is still
                    // other people's notifications. The row keeps its attempt count and comes back on a later
                    // sweep, or is dead-lettered once the budget is spent.
                    _logger.LogError(
                        ex,
                        "Outbox row {OutboxId} ({Code}) failed; it will be retried",
                        row.Id, row.EventCode);

                    await _outbox.MarkFailedAsync(row.Id, ex.Message, DateTime.UtcNow, cancellationToken);
                }
            }

            _logger.LogInformation(
                "Notification outbox sweep: {Sent}/{Claimed} rows dispatched", sent, claimed.Count);

            return sent;
        }

        private async Task ProcessAsync(
            NotificationOutboxEntry row,
            IReadOnlyDictionary<Guid, PersonInfo> recipients,
            CancellationToken cancellationToken)
        {
            // Throws when the code has no entry, which fails this row rather than sending something nobody
            // specified on channels nobody chose.
            var descriptor = NotificationEventCatalog.Describe(row.EventCode);

            var parameters = Deserialize(row.ParametersJson);
            recipients.TryGetValue(row.RecipientId, out var person);

            var written = _copy.Write(row.EventCode, parameters);

            var command = new SendNotificationCommand(
                RecipientId: row.RecipientId,
                Type: NotificationEventCatalog.NotificationTypeFor(row.EventCode),
                Channel: descriptor.Channels,
                Subject: written.Subject,
                Body: written.Body,
                Priority: descriptor.Criticality == NotificationCriticality.Critical
                    ? NotificationPriority.High
                    : NotificationPriority.Normal,
                PlainTextBody: written.PlainTextBody,
                RecipientPhone: person?.PhoneNumber,
                RecipientName: FullName(person),
                BookingId: SubjectIdFor(row, "Booking"),
                PaymentId: SubjectIdFor(row, "Payment"),
                ProviderId: SubjectIdFor(row, "Provider"),

                // The intent's own identity is the de-duplication scope downstream, so a row swept twice —
                // after a lease expiry, say — still notifies once per channel.
                IdempotencyKey: row.Id,
                EventCode: row.EventCode);

            await _mediator.Send(command, cancellationToken);
        }

        private static Guid? SubjectIdFor(NotificationOutboxEntry row, string subjectType) =>
            string.Equals(row.SubjectType, subjectType, StringComparison.Ordinal) ? row.SubjectId : null;

        /// <summary>The recipient's real name — never the «مشتری <digits>» an OTP sign-up without a name stores.</summary>
        private static string? FullName(PersonInfo? person) =>
            person is null ? null : PersonName.RealOrNull(person.FirstName, person.LastName);

        private static IReadOnlyDictionary<string, string> Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, string>();

            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
    }
}
