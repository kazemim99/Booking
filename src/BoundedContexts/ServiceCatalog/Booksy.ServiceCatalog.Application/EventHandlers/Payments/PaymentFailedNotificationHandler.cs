// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Payments/PaymentFailedNotificationHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Application.Commands.Notifications.SendNotification;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.Payments
{
    /// <summary>
    /// Handles PaymentFailedEvent and sends payment failure notifications
    /// </summary>
    public sealed class PaymentFailedNotificationHandler : IDomainEventHandler<PaymentFailedEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<PaymentFailedNotificationHandler> _logger;

        public PaymentFailedNotificationHandler(
            ISender mediator,
            ILogger<PaymentFailedNotificationHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task HandleAsync(PaymentFailedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling PaymentFailedEvent for PaymentId: {PaymentId}", notification.PaymentId);

            try
            {
                await SendCustomerNotificationAsync(notification, cancellationToken);

                _logger.LogInformation("Payment failed notification sent successfully for PaymentId: {PaymentId}",
                    notification.PaymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment failed notification for PaymentId: {PaymentId}",
                    notification.PaymentId);
                // Don't throw - notifications are not critical
            }
        }

        private async Task SendCustomerNotificationAsync(PaymentFailedEvent notification, CancellationToken cancellationToken)
        {
            var subject = "Payment Failed";
            var body = $@"
                <h2 style='color: #f44336;'>Payment Failed</h2>
                <p>We were unable to process your payment.</p>

                <div style='background-color: #ffebee; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #f44336;'>
                    <h3>Payment Details</h3>
                    <p><strong>Payment ID:</strong> {notification.PaymentId.Value}</p>
                    <p><strong>Failed At:</strong> {notification.FailedAt:yyyy-MM-dd HH:mm}</p>
                    {(notification.BookingId != null ? $"<p><strong>Booking ID:</strong> {notification.BookingId.Value}</p>" : "")}
                    <p><strong>Reason:</strong> {notification.FailureReason}</p>
                </div>

                <h3>What to do next:</h3>
                <ul>
                    <li>Check that your payment method has sufficient funds</li>
                    <li>Verify that your payment information is up to date</li>
                    <li>Try using a different payment method</li>
                    <li>Contact your bank if the problem persists</li>
                </ul>

                <p style='text-align: center; margin: 30px 0;'>
                    <a href='#' style='background-color: #4CAF50; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                        Retry Payment
                    </a>
                </p>

                <p>If you continue to experience issues, please contact our support team.</p>
            ";

            var plainTextBody = $@"
                Payment Failed

                We were unable to process your payment.

                Payment ID: {notification.PaymentId.Value}
                Failed At: {notification.FailedAt:yyyy-MM-dd HH:mm}
                {(notification.BookingId != null ? $"Booking ID: {notification.BookingId.Value}" : "")}
                Reason: {notification.FailureReason}

                What to do next:
                - Check that your payment method has sufficient funds
                - Verify that your payment information is up to date
                - Try using a different payment method
                - Contact your bank if the problem persists

                If you continue to experience issues, please contact our support team.
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.CustomerId.Value,
                Type: NotificationType.PaymentFailed,
                Channel: NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.InApp,
                Subject: subject,
                Body: body,
                Priority: NotificationPriority.High,
                PlainTextBody: plainTextBody,
                BookingId: notification.BookingId?.Value,
                PaymentId: notification.PaymentId.Value);

            await _mediator.Send(command, cancellationToken);
        }
    }
}
