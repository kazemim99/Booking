// ========================================
// Booksy.ServiceCatalog.Infrastructure/BackgroundJobs/ProcessScheduledNotificationsJob.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Background job that processes scheduled notifications
    /// Runs periodically (every minute) to send notifications that are due
    /// </summary>
    /// <remarks>
    /// Sending is delegated to <see cref="INotificationDispatcher"/>, so a scheduled reminder is subject to the
    /// same preference gate, de-duplication and retry/dead-letter rules as any other notification. The job used
    /// to fan out to the channel services itself and switched on the combined channel value, which threw
    /// "channel is not supported" for anything scheduled on more than one channel.
    /// </remarks>
    public class ProcessScheduledNotificationsJob
    {
        private readonly INotificationReadRepository _readRepo;
        private readonly INotificationWriteRepository _writeRepo;
        private readonly INotificationDispatcher _dispatcher;
        private readonly ILogger<ProcessScheduledNotificationsJob> _logger;

        public ProcessScheduledNotificationsJob(
            INotificationReadRepository readRepo,
            INotificationWriteRepository writeRepo,
            INotificationDispatcher dispatcher,
            ILogger<ProcessScheduledNotificationsJob> logger)
        {
            _readRepo = readRepo;
            _writeRepo = writeRepo;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        /// <summary>
        /// Executes the job to process scheduled notifications
        /// </summary>
        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Starting ProcessScheduledNotificationsJob execution");

                // Get notifications due now
                var notifications = await _readRepo.GetScheduledNotificationsDueAsync(ct);

                _logger.LogInformation(
                    "Found {Count} scheduled notifications due for processing",
                    notifications.Count);

                foreach (var notification in notifications)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Processing scheduled notification {NotificationId} for user {RecipientId}, Channel: {Channel}",
                            notification.Id.Value,
                            notification.RecipientId.Value,
                            notification.Channel);

                        var outcome = await _dispatcher.DispatchAsync(notification, ct);

                        _logger.LogInformation(
                            "Scheduled notification {NotificationId} finished as {Status} " +
                            "(delivered {Delivered}, skipped {Skipped}, failed {Failed})",
                            notification.Id.Value,
                            notification.Status,
                            outcome.Delivered,
                            outcome.Skipped,
                            outcome.Failed);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to send scheduled notification {NotificationId} for user {RecipientId}",
                            notification.Id.Value,
                            notification.RecipientId.Value);
                    }
                    finally
                    {
                        // Update notification status in database
                        await _writeRepo.UpdateNotificationAsync(notification, ct);
                    }
                }

                _logger.LogInformation(
                    "Completed ProcessScheduledNotificationsJob execution. Processed {Count} notifications",
                    notifications.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing ProcessScheduledNotificationsJob");
                throw;
            }
        }
    }
}
