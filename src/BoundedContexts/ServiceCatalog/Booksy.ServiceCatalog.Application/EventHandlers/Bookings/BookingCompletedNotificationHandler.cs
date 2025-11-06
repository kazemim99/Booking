// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Bookings/BookingCompletedNotificationHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Application.Commands.Notifications.ScheduleNotification;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.Bookings
{
    /// <summary>
    /// Handles BookingCompletedEvent and sends review request notification
    /// </summary>
    public sealed class BookingCompletedNotificationHandler : IDomainEventHandler<BookingCompletedEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<BookingCompletedNotificationHandler> _logger;

        public BookingCompletedNotificationHandler(
            ISender mediator,
            ILogger<BookingCompletedNotificationHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task HandleAsync(BookingCompletedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling BookingCompletedEvent for BookingId: {BookingId}", notification.BookingId);

            try
            {
                // Schedule review request notification for 2 hours after completion
                var scheduledFor = notification.CompletedAt.AddHours(2);

                var subject = "How was your experience?";
                var body = $@"
                    <h2>We hope you enjoyed your service!</h2>
                    <p>Your recent appointment has been completed. We'd love to hear about your experience.</p>

                    <p><strong>Booking ID:</strong> {notification.BookingId.Value}</p>
                    <p><strong>Service Date:</strong> {notification.ScheduledTime:yyyy-MM-dd HH:mm}</p>

                    <p style='text-align: center; margin: 30px 0;'>
                        <a href='#' style='background-color: #4CAF50; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Leave a Review
                        </a>
                    </p>

                    <p>Your feedback helps us improve and helps other customers make informed decisions.</p>
                    <p>Thank you for choosing our service!</p>
                ";

                var plainTextBody = $@"
                    We hope you enjoyed your service!

                    Your recent appointment has been completed. We'd love to hear about your experience.

                    Booking ID: {notification.BookingId.Value}
                    Service Date: {notification.ScheduledTime:yyyy-MM-dd HH:mm}

                    Please visit our website to leave a review.
                    Your feedback helps us improve!
                ";

                var command = new ScheduleNotificationCommand(
                    RecipientId: notification.CustomerId.Value,
                    Type: NotificationType.ReviewRequest,
                    Channel: NotificationChannel.Email | NotificationChannel.InApp,
                    Subject: subject,
                    Body: body,
                    ScheduledFor: scheduledFor,
                    Priority: NotificationPriority.Low,
                    PlainTextBody: plainTextBody,
                    BookingId: notification.BookingId.Value,
                    ProviderId: notification.ProviderId.Value);

                await _mediator.Send(command, cancellationToken);

                _logger.LogInformation("Review request notification scheduled for BookingId: {BookingId} at {ScheduledFor}",
                    notification.BookingId, scheduledFor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule review request notification for BookingId: {BookingId}",
                    notification.BookingId);
                // Don't throw - notifications are not critical for booking flow
            }
        }
    }
}
