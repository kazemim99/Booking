// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Bookings/BookingRescheduledNotificationHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Application.Commands.Notifications.SendNotification;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.Bookings
{
    /// <summary>
    /// Handles BookingRescheduledEvent and sends rescheduling notifications
    /// </summary>
    public sealed class BookingRescheduledNotificationHandler : IDomainEventHandler<BookingRescheduledEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<BookingRescheduledNotificationHandler> _logger;

        public BookingRescheduledNotificationHandler(
            ISender mediator,
            ILogger<BookingRescheduledNotificationHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task HandleAsync(BookingRescheduledEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling BookingRescheduledEvent for OldBookingId: {OldBookingId}, NewBookingId: {NewBookingId}",
                notification.OldBookingId, notification.NewBookingId);

            try
            {
                // Send notification to customer
                await SendCustomerNotificationAsync(notification, cancellationToken);

                // Send notification to provider
                await SendProviderNotificationAsync(notification, cancellationToken);

                _logger.LogInformation("Booking rescheduled notifications sent successfully for BookingId: {NewBookingId}",
                    notification.NewBookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking rescheduled notifications for BookingId: {NewBookingId}",
                    notification.NewBookingId);
                // Don't throw - notifications are not critical for booking flow
            }
        }

      

        private async Task SendCustomerNotificationAsync(BookingRescheduledEvent notification, CancellationToken cancellationToken)
        {
            var reasonMessage = !string.IsNullOrEmpty(notification.Reason)
                ? $"<p><strong>Reason:</strong> {notification.Reason}</p>"
                : "";

            var subject = "Booking Rescheduled";
            var body = $@"
                <h2>Your booking has been rescheduled</h2>
                <p><strong>New Booking ID:</strong> {notification.NewBookingId.Value}</p>

                <h3>Previous Schedule:</h3>
                <p>Date & Time: {notification.OldStartTime:yyyy-MM-dd HH:mm}</p>

                <h3>New Schedule:</h3>
                <p>Date & Time: {notification.NewStartTime:yyyy-MM-dd HH:mm}</p>

                {reasonMessage}

                <p>We look forward to seeing you at the new time!</p>
                <p>If you need to make further changes, please contact us.</p>
            ";

            var plainTextBody = $@"
                Your booking has been rescheduled

                New Booking ID: {notification.NewBookingId.Value}

                Previous Schedule:
                Date & Time: {notification.OldStartTime:yyyy-MM-dd HH:mm}

                New Schedule:
                Date & Time: {notification.NewStartTime:yyyy-MM-dd HH:mm}

                {(string.IsNullOrEmpty(notification.Reason) ? "" : $"Reason: {notification.Reason}")}
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.CustomerId.Value,
                Type: NotificationType.BookingRescheduled,
                Channel: NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.InApp,
                Subject: subject,
                Body: body,
                Priority: NotificationPriority.High,
                PlainTextBody: plainTextBody,
                BookingId: notification.NewBookingId.Value,
                ProviderId: notification.ProviderId.Value);

            await _mediator.Send(command, cancellationToken);
        }

        private async Task SendProviderNotificationAsync(BookingRescheduledEvent notification, CancellationToken cancellationToken)
        {
            var subject = "Booking Rescheduled";
            var body = $@"
                <h2>A booking has been rescheduled</h2>
                <ul>
                    <li><strong>New Booking ID:</strong> {notification.NewBookingId.Value}</li>
                    <li><strong>Old Booking ID:</strong> {notification.OldBookingId.Value}</li>
                    <li><strong>Customer ID:</strong> {notification.CustomerId.Value}</li>
                    <li><strong>Previous Time:</strong> {notification.OldStartTime:yyyy-MM-dd HH:mm}</li>
                    <li><strong>New Time:</strong> {notification.NewStartTime:yyyy-MM-dd HH:mm}</li>
                </ul>
                <p>Please update your schedule accordingly.</p>
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.ProviderId.Value,
                Type: NotificationType.BookingRescheduled,
                Channel: NotificationChannel.Email | NotificationChannel.InApp,
                Subject: subject,
                Body: body,
                Priority: NotificationPriority.Normal,
                BookingId: notification.NewBookingId.Value,
                ProviderId: notification.ProviderId.Value);

            await _mediator.Send(command, cancellationToken);
        }
    }
}
