// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Bookings/BookingNoShowNotificationHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Application.Commands.Notifications.SendNotification;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.Bookings
{
    /// <summary>
    /// Handles BookingNoShowEvent and sends no-show notifications
    /// </summary>
    public sealed class BookingNoShowNotificationHandler : IDomainEventHandler<BookingNoShowEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<BookingNoShowNotificationHandler> _logger;

        public BookingNoShowNotificationHandler(
            ISender mediator,
            ILogger<BookingNoShowNotificationHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(BookingNoShowEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling BookingNoShowEvent for BookingId: {BookingId}", notification.BookingId);

            try
            {
                // Send notification to customer
                await SendCustomerNotificationAsync(notification, cancellationToken);

                _logger.LogInformation("Booking no-show notification sent successfully for BookingId: {BookingId}",
                    notification.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking no-show notification for BookingId: {BookingId}",
                    notification.BookingId);
                // Don't throw - notifications are not critical for booking flow
            }
        }

        private async Task SendCustomerNotificationAsync(BookingNoShowEvent notification, CancellationToken cancellationToken)
        {
            var feeMessage = notification.ForfeitedAmount.Amount > 0
                ? $"<p><strong>No-Show Fee:</strong> ${notification.ForfeitedAmount.Amount:F2}</p>"
                : "";

            var subject = "Missed Appointment - No Show";
            var body = $@"
                <h2>Missed Appointment Notice</h2>
                <p>You were marked as a no-show for your scheduled appointment.</p>

                <p><strong>Booking ID:</strong> {notification.BookingId.Value}</p>
                <p><strong>Scheduled Time:</strong> {notification.ScheduledTime:yyyy-MM-dd HH:mm}</p>
                {feeMessage}

                <p>If you believe this was an error, please contact us immediately.</p>
                <p>To avoid no-show fees in the future, please cancel appointments at least 24 hours in advance.</p>
            ";

            var plainTextBody = $@"
                Missed Appointment Notice

                You were marked as a no-show for your scheduled appointment.

                Booking ID: {notification.BookingId.Value}
                Scheduled Time: {notification.ScheduledTime:yyyy-MM-dd HH:mm}
                No-Show Fee: ${notification.ForfeitedAmount.Amount:F2}

                If you believe this was an error, please contact us immediately.
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.CustomerId.Value,
                Type: NotificationType.BookingNoShow,
                Channel: NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.InApp,
                Subject: subject,
                Body: body,
                Priority: NotificationPriority.High,
                PlainTextBody: plainTextBody,
                BookingId: notification.BookingId.Value,
                ProviderId: notification.ProviderId.Value);

            await _mediator.Send(command, cancellationToken);
        }
    }
}
