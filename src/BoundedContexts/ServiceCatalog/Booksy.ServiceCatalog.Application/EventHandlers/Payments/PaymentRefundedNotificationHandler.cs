// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Payments/PaymentRefundedNotificationHandler.cs
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
    /// Handles PaymentRefundedEvent and sends refund notifications
    /// </summary>
    public sealed class PaymentRefundedNotificationHandler : IDomainEventHandler<PaymentRefundedEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<PaymentRefundedNotificationHandler> _logger;

        public PaymentRefundedNotificationHandler(
            ISender mediator,
            ILogger<PaymentRefundedNotificationHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(PaymentRefundedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling PaymentRefundedEvent for PaymentId: {PaymentId}", notification.PaymentId);

            try
            {
                await SendCustomerNotificationAsync(notification, cancellationToken);

                _logger.LogInformation("Payment refund notification sent successfully for PaymentId: {PaymentId}",
                    notification.PaymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment refund notification for PaymentId: {PaymentId}",
                    notification.PaymentId);
                // Don't throw - notifications are not critical
            }
        }

        private async Task SendCustomerNotificationAsync(PaymentRefundedEvent notification, CancellationToken cancellationToken)
        {
            var subject = "Refund Processed";
            var body = $@"
                <h2 style='color: #2196F3;'>Refund Processed Successfully</h2>
                <p>Your refund has been processed and will appear in your account shortly.</p>

                <div style='background-color: #e3f2fd; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #2196F3;'>
                    <h3>Refund Details</h3>
                    <table style='width: 100%;'>
                        <tr>
                            <td><strong>Payment ID:</strong></td>
                            <td>{notification.PaymentId.Value}</td>
                        </tr>
                        <tr>
                            <td><strong>Refund Amount:</strong></td>
                            <td style='font-size: 24px; color: #2196F3;'>${notification.RefundAmount.Amount:F2} {notification.RefundAmount.Currency}</td>
                        </tr>
                        <tr>
                            <td><strong>Processed At:</strong></td>
                            <td>{notification.RefundedAt:yyyy-MM-dd HH:mm}</td>
                        </tr>
                        <tr>
                            <td><strong>Reason:</strong></td>
                            <td>{notification.Reason}</td>
                        </tr>
                        {(notification.BookingId.HasValue ? $@"
                        <tr>
                            <td><strong>Booking ID:</strong></td>
                            <td>{notification.BookingId.Value.Value}</td>
                        </tr>" : "")}
                    </table>
                </div>

                <h3>What happens next?</h3>
                <ul>
                    <li>The refund will be credited to your original payment method</li>
                    <li>It may take 5-10 business days to appear in your account</li>
                    <li>You will receive confirmation once the funds are available</li>
                </ul>

                <p>If you have any questions about this refund, please contact our support team.</p>
            ";

            var plainTextBody = $@"
                Refund Processed Successfully

                Your refund has been processed and will appear in your account shortly.

                Refund Details:
                Payment ID: {notification.PaymentId.Value}
                Refund Amount: ${notification.RefundAmount.Amount:F2} {notification.RefundAmount.Currency}
                Processed At: {notification.RefundedAt:yyyy-MM-dd HH:mm}
                Reason: {notification.Reason}
                {(notification.BookingId.HasValue ? $"Booking ID: {notification.BookingId.Value.Value}" : "")}

                What happens next?
                - The refund will be credited to your original payment method
                - It may take 5-10 business days to appear in your account
                - You will receive confirmation once the funds are available

                If you have any questions about this refund, please contact our support team.
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.CustomerId.Value,
                Type: NotificationType.RefundProcessed,
                Channel: NotificationChannel.Email | NotificationChannel.InApp,
                Subject: subject,
                Body: body,
                Priority: NotificationPriority.High,
                PlainTextBody: plainTextBody,
                BookingId: notification.BookingId?.Value,
                ProviderId: notification.ProviderId.Value,
                PaymentId: notification.PaymentId.Value);

            await _mediator.Send(command, cancellationToken);
        }
    }
}
