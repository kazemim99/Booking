// ========================================
// Booksy.ServiceCatalog.Application/Commands/Notifications/SendBulkNotification/SendBulkNotificationCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Notifications.SendBulkNotification
{
    /// <summary>
    /// Handler for SendBulkNotificationCommand
    /// </summary>
    public sealed class SendBulkNotificationCommandHandler
        : ICommandHandler<SendBulkNotificationCommand, SendBulkNotificationResult>
    {
        private readonly INotificationWriteRepository _notificationRepository;
        private readonly ILogger<SendBulkNotificationCommandHandler> _logger;

        public SendBulkNotificationCommandHandler(
            INotificationWriteRepository notificationRepository,
            ILogger<SendBulkNotificationCommandHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<SendBulkNotificationResult> Handle(
            SendBulkNotificationCommand command,
            CancellationToken cancellationToken)
        {
            // Validate recipients
            if (command.RecipientIds == null || !command.RecipientIds.Any())
            {
                throw new DomainValidationException(
                    nameof(command.RecipientIds),
                    "No recipients provided");
            }

            // Limit batch size
            if (command.RecipientIds.Count > 1000)
            {
                throw new DomainValidationException(
                    nameof(command.RecipientIds),
                    "Maximum 1000 recipients per batch. Please split into multiple batches.");
            }

            // Generate unique batch ID
            var batchId = Guid.NewGuid().ToString();

            var notificationIds = new List<Guid>();
            var errors = new List<string>();
            var successCount = 0;
            var failureCount = 0;

            // Process each recipient
            foreach (var recipientId in command.RecipientIds)
            {
                try
                {
                    // Create notification for this recipient
                    var notification = Notification.Create(
                        UserId.From(recipientId),
                        command.Type,
                        command.Channel,
                        command.Subject,
                        command.Body,
                        command.Priority,
                        command.PlainTextBody);

                    // Set template if provided
                    if (!string.IsNullOrWhiteSpace(command.TemplateId) && command.TemplateData != null)
                    {
                        notification.SetTemplate(command.TemplateId, command.TemplateData);
                    }

                    // Set campaign and batch ID
                    notification.SetCampaign(command.CampaignId, batchId);

                    // Add metadata
                    if (command.Metadata != null)
                    {
                        foreach (var kvp in command.Metadata)
                        {
                            notification.AddMetadata(kvp.Key, kvp.Value);
                        }
                    }

                    // Queue the notification
                    notification.Queue();

                    // Save to database
                    await _notificationRepository.SaveNotificationAsync(notification, cancellationToken);

                    notificationIds.Add(notification.Id.Value);

                    // Left Queued for NotificationProcessorService to deliver. A bulk send used to fan out on a
                    // detached Task.Run per recipient, which outlived the request scope and kept using its
                    // disposed DbContext — and skipped the preference gate, de-duplication and retry rules that
                    // the dispatcher enforces. The sweep applies all of them, and a batch of up to 1000 sends no
                    // longer races the caller's request.
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to create notification for recipient: RecipientId={RecipientId}",
                        recipientId);
                    errors.Add($"Recipient {recipientId}: {ex.Message}");
                    failureCount++;
                }
            }

            _logger.LogInformation(
                "Bulk notification initiated: BatchId={BatchId}, Total={Total}, Success={Success}, Failure={Failure}",
                batchId,
                command.RecipientIds.Count,
                successCount,
                failureCount);

            return new SendBulkNotificationResult(
                batchId,
                command.RecipientIds.Count, // TotalCount
                command.RecipientIds.Count, // TotalRecipients
                successCount,
                failureCount,
                notificationIds,
                errors);
        }

    }
}
