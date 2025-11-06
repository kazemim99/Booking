// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Payments/PaymentProcessedNotificationHandler.cs
// ========================================


namespace Booksy.ServiceCatalog.Application.EventHandlers.Payments
{
    /// <summary>
    /// Handles PaymentProcessedEvent and sends payment receipt notifications
    /// </summary>
    public sealed class PaymentProcessedNotificationHandler : IDomainEventHandler<PaymentProcessedEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<PaymentProcessedNotificationHandler> _logger;

        public PaymentProcessedNotificationHandler(
            ISender mediator,
            ILogger<PaymentProcessedNotificationHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task HandleAsync(PaymentProcessedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling PaymentProcessedEvent for PaymentId: {PaymentId}", notification.PaymentId);

            try
            {
                // Send payment receipt to customer
                await SendCustomerReceiptAsync(notification, cancellationToken);

                // Send payment notification to provider
                await SendProviderNotificationAsync(notification, cancellationToken);

                _logger.LogInformation("Payment processed notifications sent successfully for PaymentId: {PaymentId}",
                    notification.PaymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment processed notifications for PaymentId: {PaymentId}",
                    notification.PaymentId);
                // Don't throw - notifications are not critical
            }
        }

        private async Task SendCustomerReceiptAsync(PaymentProcessedEvent notification, CancellationToken cancellationToken)
        {
            var subject = "Payment Receipt";
            var body = $@"
                <h2>Payment Successful</h2>
                <p>Thank you for your payment!</p>

                <div style='background-color: #f5f5f5; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                    <h3>Payment Details</h3>
                    <table style='width: 100%;'>
                        <tr>
                            <td><strong>Payment ID:</strong></td>
                            <td>{notification.PaymentId.Value}</td>
                        </tr>
                        <tr>
                            <td><strong>Amount:</strong></td>
                            <td style='font-size: 24px; color: #4CAF50;'>${notification.Amount.Amount:F2} {notification.Amount.Currency}</td>
                        </tr>
                        <tr>
                            <td><strong>Date:</strong></td>
                            <td>{notification.ProcessedAt:yyyy-MM-dd HH:mm}</td>
                        </tr>
                        {(notification.BookingId.HasValue ? $@"
                        <tr>
                            <td><strong>Booking ID:</strong></td>
                            <td>{notification.BookingId.Value.Value}</td>
                        </tr>" : "")}
                    </table>
                </div>

                <p>This is your official receipt. Please keep it for your records.</p>
                <p>If you have any questions about this payment, please contact us.</p>
            ";

            var plainTextBody = $@"
                Payment Successful

                Thank you for your payment!

                Payment Details:
                Payment ID: {notification.PaymentId.Value}
                Amount: ${notification.Amount.Amount:F2} {notification.Amount.Currency}
                Date: {notification.ProcessedAt:yyyy-MM-dd HH:mm}
                {(notification.BookingId.HasValue ? $"Booking ID: {notification.BookingId.Value.Value}" : "")}

                This is your official receipt.
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.CustomerId.Value,
                Type: NotificationType.PaymentReceived,
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

        private async Task SendProviderNotificationAsync(PaymentProcessedEvent notification, CancellationToken cancellationToken)
        {
            var subject = "Payment Received";
            var body = $@"
                <h2>Payment Received</h2>
                <p>A payment has been successfully processed.</p>

                <ul>
                    <li><strong>Payment ID:</strong> {notification.PaymentId.Value}</li>
                    <li><strong>Customer ID:</strong> {notification.CustomerId.Value}</li>
                    <li><strong>Amount:</strong> ${notification.Amount.Amount:F2} {notification.Amount.Currency}</li>
                    <li><strong>Date:</strong> {notification.ProcessedAt:yyyy-MM-dd HH:mm}</li>
                    {(notification.BookingId.HasValue ? $"<li><strong>Booking ID:</strong> {notification.BookingId.Value.Value}</li>" : "")}
                </ul>

                <p>The funds will be included in your next payout.</p>
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.ProviderId.Value,
                Type: NotificationType.PaymentReceived,
                Channel: NotificationChannel.Email | NotificationChannel.InApp,
                Subject: subject,
                Body: body,
                Priority: NotificationPriority.Normal,
                BookingId: notification.BookingId?.Value,
                ProviderId: notification.ProviderId.Value,
                PaymentId: notification.PaymentId.Value);

            await _mediator.Send(command, cancellationToken);
        }
    }
}
