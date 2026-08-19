using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications
{
    /// <summary>
    /// De-duplication gate and delivery log backed by the <c>NotificationDeliveries</c> table's composite PK.
    /// </summary>
    /// <remarks>
    /// Every operation runs in its <b>own</b> DbContext/scope so the claim is committed immediately and visible to
    /// a concurrent dispatcher — a claim entangled with the caller's unit of work would only become visible when
    /// that transaction commits, which is far too late to stop a duplicate send.
    /// </remarks>
    public sealed class NotificationDeliveryLog : INotificationDeliveryLog
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationDeliveryLog> _logger;

        public NotificationDeliveryLog(IServiceScopeFactory scopeFactory, ILogger<NotificationDeliveryLog> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<NotificationDeliveryClaim> TryClaimAsync(
            Guid eventId,
            NotificationChannel channel,
            string recipient,
            Guid notificationId,
            CancellationToken cancellationToken = default)
        {
            var channelName = channel.ToString();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

                // 1) Optimistically claim the tuple with an INSERT. The composite PK lets exactly one concurrent
                //    dispatcher win; the rest get a unique/PK violation.
                if (await TryInsertAsync(db, eventId, channelName, recipient, notificationId, cancellationToken))
                    return NotificationDeliveryClaim.Claimed;

                // 2) The tuple is taken — decide from the existing row whether this is a duplicate or a retry.
                db.ChangeTracker.Clear();
                var existing = await db.NotificationDeliveries.AsNoTracking().FirstOrDefaultAsync(
                    d => d.EventId == eventId && d.Channel == channelName && d.Recipient == recipient,
                    cancellationToken);

                if (existing is null)
                {
                    // Raced with a cleanup between the failed INSERT and this read; one more attempt to claim.
                    return await TryInsertAsync(db, eventId, channelName, recipient, notificationId, cancellationToken)
                        ? NotificationDeliveryClaim.Claimed
                        : NotificationDeliveryClaim.AlreadyDelivered;
                }

                if (existing.Status == NotificationDelivery.Delivered)
                    return NotificationDeliveryClaim.AlreadyDelivered;

                // Pending or Failed: a previous attempt did not land. Re-claiming keeps the notification
                // retryable — treating "seen before" as "already delivered" would turn one transient gateway
                // error into a message the customer never receives.
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE ""ServiceCatalog"".""NotificationDeliveries""
                       SET ""Status"" = 'Pending', ""AttemptCount"" = ""AttemptCount"" + 1,
                           ""NotificationId"" = {notificationId}, ""UpdatedAt"" = now()
                       WHERE ""EventId"" = {eventId} AND ""Channel"" = {channelName} AND ""Recipient"" = {recipient}
                         AND ""Status"" <> 'Delivered'",
                    cancellationToken);

                return NotificationDeliveryClaim.Claimed;
            }
            catch (Exception ex)
            {
                // Fail open: with the dedup store unreachable, a duplicate notification is a far smaller harm
                // than a silently dropped booking confirmation or refund receipt. Logged so the gap is visible.
                _logger.LogError(ex,
                    "Delivery-log claim failed for notification {NotificationId} on {Channel}; sending without de-duplication",
                    notificationId, channel);
                return NotificationDeliveryClaim.Claimed;
            }
        }

        public async Task RecordOutcomeAsync(
            Guid eventId,
            NotificationChannel channel,
            string recipient,
            bool success,
            string? gatewayMessageId,
            string? errorMessage,
            CancellationToken cancellationToken = default)
        {
            var channelName = channel.ToString();
            var status = success ? NotificationDelivery.Delivered : NotificationDelivery.Failed;
            var error = Truncate(errorMessage, 1000);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

                // Interpolated, not raw-with-object[]: a null message id or error must be passed as a typed null.
                // Boxing it into DBNull.Value gives Npgsql a parameter it cannot infer a type for, and the whole
                // UPDATE throws — which the catch below would swallow, leaving every tuple stuck on 'Pending' and
                // silently disabling de-duplication.
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE ""ServiceCatalog"".""NotificationDeliveries""
                       SET ""Status"" = {status}, ""GatewayMessageId"" = {gatewayMessageId},
                           ""ErrorMessage"" = {error}, ""UpdatedAt"" = now()
                       WHERE ""EventId"" = {eventId} AND ""Channel"" = {channelName} AND ""Recipient"" = {recipient}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // The send already happened; losing the log entry must not fail the dispatch. The worst case is
                // that a later retry re-sends this one message, which the notification's own state still bounds.
                _logger.LogError(ex,
                    "Could not record delivery outcome for event {EventId} on {Channel}", eventId, channel);
            }
        }

        private static async Task<bool> TryInsertAsync(
            ServiceCatalogDbContext db,
            Guid eventId,
            string channelName,
            string recipient,
            Guid notificationId,
            CancellationToken cancellationToken)
        {
            db.NotificationDeliveries.Add(new NotificationDelivery
            {
                EventId = eventId,
                Channel = channelName,
                Recipient = recipient,
                NotificationId = notificationId,
                Status = NotificationDelivery.Pending,
                AttemptCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear(); // the row already exists — not our claim
                return false;
            }
        }

        private static string? Truncate(string? value, int max) =>
            value is null || value.Length <= max ? value : value[..max];
    }
}
