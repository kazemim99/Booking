// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Bookings/BookingConfirmedNotificationHandler.cs
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
    /// Handles BookingConfirmedEvent and sends notifications to customer and provider
    /// </summary>
    public sealed class BookingConfirmedNotificationHandler : IDomainEventHandler<BookingConfirmedEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<BookingConfirmedNotificationHandler> _logger;

        public BookingConfirmedNotificationHandler(
            ISender mediator,
            ILogger<BookingConfirmedNotificationHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task HandleAsync(BookingConfirmedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling BookingConfirmedEvent for BookingId: {BookingId}", notification.BookingId);

            try
            {
                // Send notification to customer
                await SendCustomerNotificationAsync(notification, cancellationToken);

                // Send notification to provider
                await SendProviderNotificationAsync(notification, cancellationToken);

                _logger.LogInformation("Booking confirmed notifications sent successfully for BookingId: {BookingId}",
                    notification.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking confirmed notifications for BookingId: {BookingId}",
                    notification.BookingId);
                // Don't throw - notifications are not critical for booking flow
            }
        }

        private async Task SendCustomerNotificationAsync(BookingConfirmedEvent notification, CancellationToken cancellationToken)
        {
            var subject = "Booking Confirmed";
            var body = $@"
                <h2>Your booking has been confirmed!</h2>
                <p>Booking Details:</p>
                <ul>
                    <li><strong>Booking ID:</strong> {notification.BookingId.Value}</li>
                    <li><strong>Date & Time:</strong> {notification.StartTime:yyyy-MM-dd HH:mm}</li>
                    <li><strong>Duration:</strong> {(notification.EndTime - notification.StartTime).TotalMinutes} minutes</li>
                </ul>
                <p>We look forward to seeing you!</p>
                <p>If you need to make changes, please contact us as soon as possible.</p>
            ";

            var plainTextBody = $@"
                Your booking has been confirmed!

                Booking ID: {notification.BookingId.Value}
                Date & Time: {notification.StartTime:yyyy-MM-dd HH:mm}
                Duration: {(notification.EndTime - notification.StartTime).TotalMinutes} minutes

                We look forward to seeing you!
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.CustomerId.Value,
                Type: NotificationType.BookingConfirmation,
                Channel: NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.InApp,
                Subject: subject,
                Body: body,
                Priority: NotificationPriority.High,
                PlainTextBody: plainTextBody,
                BookingId: notification.BookingId.Value,
                ProviderId: notification.ProviderId.Value);

            await _mediator.Send(command, cancellationToken);
        }

        private async Task SendProviderNotificationAsync(BookingConfirmedEvent notification, CancellationToken cancellationToken)
        {
            var subject = "New Booking Confirmed";
            var body = $@"
                <h2>A new booking has been confirmed</h2>
                <p>Booking Details:</p>
                <ul>
                    <li><strong>Booking ID:</strong> {notification.BookingId.Value}</li>
                    <li><strong>Customer ID:</strong> {notification.CustomerId.Value}</li>
                    <li><strong>Service ID:</strong> {notification.ServiceId.Value}</li>
                    <li><strong>Staff ID:</strong> {notification.StaffId}</li>
                    <li><strong>Date & Time:</strong> {notification.StartTime:yyyy-MM-dd HH:mm}</li>
                    <li><strong>Duration:</strong> {(notification.EndTime - notification.StartTime).TotalMinutes} minutes</li>
                </ul>
                <p>Please ensure you're prepared for this appointment.</p>
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.ProviderId.Value,
                Type: NotificationType.NewBooking,
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
