// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Bookings/BookingCancelledNotificationHandler.cs
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
    /// Handles BookingCancelledEvent and sends cancellation notifications
    /// </summary>
    public sealed class BookingCancelledNotificationHandler : IDomainEventHandler<BookingCancelledEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<BookingCancelledNotificationHandler> _logger;

        public BookingCancelledNotificationHandler(
            ISender mediator,
            ILogger<BookingCancelledNotificationHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task HandleAsync(BookingCancelledEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling BookingCancelledEvent for BookingId: {BookingId}", notification.BookingId);

            try
            {
                // Send notification to customer
                await SendCustomerNotificationAsync(notification, cancellationToken);

                // Send notification to provider
                await SendProviderNotificationAsync(notification, cancellationToken);

                _logger.LogInformation("Booking cancelled notifications sent successfully for BookingId: {BookingId}",
                    notification.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking cancelled notifications for BookingId: {BookingId}",
                    notification.BookingId);
                // Don't throw - notifications are not critical for booking flow
            }
        }

        private async Task SendCustomerNotificationAsync(BookingCancelledEvent notification, CancellationToken cancellationToken)
        {
            var cancelledBy = notification.ByProvider ? "provider" : "you";
            var feeMessage = notification.CancellationFee > 0
                ? $"<p><strong>Cancellation Fee:</strong> ${notification.CancellationFee:F2}</p>"
                : "<p>No cancellation fee applied.</p>";

            var subject = "Booking Cancelled";
            var body = $@"
                <h2>Your booking has been cancelled</h2>
                <p>This booking was cancelled by {cancelledBy}.</p>
                <p><strong>Booking ID:</strong> {notification.BookingId.Value}</p>
                <p><strong>Reason:</strong> {notification.Reason}</p>
                <p><strong>Cancelled At:</strong> {notification.CancelledAt:yyyy-MM-dd HH:mm}</p>
                {feeMessage}
                <p>If you have any questions, please contact us.</p>
            ";

            var plainTextBody = $@"
                Your booking has been cancelled by {cancelledBy}.

                Booking ID: {notification.BookingId.Value}
                Reason: {notification.Reason}
                Cancelled At: {notification.CancelledAt:yyyy-MM-dd HH:mm}
                Cancellation Fee: ${notification.CancellationFee:F2}
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.CustomerId.Value,
                Type: NotificationType.BookingCancellation,
                Channel: NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.InApp,
                Subject: subject,
                Body: body,
                Priority: NotificationPriority.High,
                PlainTextBody: plainTextBody,
                BookingId: notification.BookingId.Value,
                ProviderId: notification.ProviderId.Value);

            await _mediator.Send(command, cancellationToken);
        }

        private async Task SendProviderNotificationAsync(BookingCancelledEvent notification, CancellationToken cancellationToken)
        {
            var cancelledBy = notification.ByProvider ? "you" : "customer";

            var subject = "Booking Cancelled";
            var body = $@"
                <h2>A booking has been cancelled</h2>
                <p>This booking was cancelled by {cancelledBy}.</p>
                <ul>
                    <li><strong>Booking ID:</strong> {notification.BookingId.Value}</li>
                    <li><strong>Customer ID:</strong> {notification.CustomerId.Value}</li>
                    <li><strong>Service ID:</strong> {notification.ServiceId.Value}</li>
                    <li><strong>Reason:</strong> {notification.Reason}</li>
                    <li><strong>Cancelled At:</strong> {notification.CancelledAt:yyyy-MM-dd HH:mm}</li>
                </ul>
                <p>Please update your schedule accordingly.</p>
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.ProviderId.Value,
                Type: NotificationType.BookingCancellation,
                Channel: NotificationChannel.Email | NotificationChannel.InApp,
                Subject: subject,
                Body: body,
                Priority: NotificationPriority.Normal,
                BookingId: notification.BookingId.Value,
                ProviderId: notification.ProviderId.Value);

            await _mediator.Send(command, cancellationToken);
        }
    }
}
