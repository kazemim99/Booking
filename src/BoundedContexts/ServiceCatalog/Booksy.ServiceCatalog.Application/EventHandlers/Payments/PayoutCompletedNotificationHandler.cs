// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Payments/PayoutCompletedNotificationHandler.cs
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
    /// Handles PayoutCompletedEvent and sends payout notifications to providers
    /// </summary>
    public sealed class PayoutCompletedNotificationHandler : IDomainEventHandler<PayoutCompletedEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<PayoutCompletedNotificationHandler> _logger;

        public PayoutCompletedNotificationHandler(
            ISender mediator,
            ILogger<PayoutCompletedNotificationHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task HandleAsync(PayoutCompletedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling PayoutCompletedEvent for PayoutId: {PayoutId}", notification.PayoutId);

            try
            {
                await SendProviderNotificationAsync(notification, cancellationToken);

                _logger.LogInformation("Payout completed notification sent successfully for PayoutId: {PayoutId}",
                    notification.PayoutId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payout completed notification for PayoutId: {PayoutId}",
                    notification.PayoutId);
                // Don't throw - notifications are not critical
            }
        }

        private async Task SendProviderNotificationAsync(PayoutCompletedEvent notification, CancellationToken cancellationToken)
        {
            var subject = "Payout Completed - Funds Transferred";
            var body = $@"
                <h2 style='color: #4CAF50;'>Payout Completed Successfully!</h2>
                <p>Your payout has been processed and the funds have been transferred to your account.</p>

                <div style='background-color: #e8f5e9; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #4CAF50;'>
                    <h3>Payout Details</h3>
                    <table style='width: 100%;'>
                        <tr>
                            <td><strong>Payout ID:</strong></td>
                            <td>{notification.PayoutId.Value}</td>
                        </tr>
                        <tr>
                            <td><strong>Amount:</strong></td>
                            <td style='font-size: 28px; color: #4CAF50; font-weight: bold;'>${notification.Amount.Amount:F2} {notification.Amount.Currency}</td>
                        </tr>
                        <tr>
                            <td><strong>Paid At:</strong></td>
                            <td>{notification.PaidAt:yyyy-MM-dd HH:mm}</td>
                        </tr>
                    </table>
                </div>

                <h3>What happens next?</h3>
                <ul>
                    <li>The funds have been transferred to your registered bank account</li>
                    <li>It may take 1-3 business days for the funds to appear in your account</li>
                    <li>You can view detailed payout information in your provider dashboard</li>
                </ul>

                <p style='text-align: center; margin: 30px 0;'>
                    <a href='#' style='background-color: #4CAF50; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                        View Payout Details
                    </a>
                </p>

                <p>Thank you for being a valued provider!</p>
                <p>If you have any questions about this payout, please contact our support team.</p>
            ";

            var plainTextBody = $@"
                Payout Completed Successfully!

                Your payout has been processed and the funds have been transferred to your account.

                Payout Details:
                Payout ID: {notification.PayoutId.Value}
                Amount: ${notification.Amount.Amount:F2} {notification.Amount.Currency}
                Paid At: {notification.PaidAt:yyyy-MM-dd HH:mm}

                What happens next?
                - The funds have been transferred to your registered bank account
                - It may take 1-3 business days for the funds to appear in your account
                - You can view detailed payout information in your provider dashboard

                Thank you for being a valued provider!
                If you have any questions about this payout, please contact our support team.
            ";

            var command = new SendNotificationCommand(
                RecipientId: notification.ProviderId.Value,
                Type: NotificationType.PayoutCompleted,
                Channel: NotificationChannel.Email | NotificationChannel.InApp,
                Subject: subject,
                Body: body,
                Priority: NotificationPriority.High,
                PlainTextBody: plainTextBody,
                ProviderId: notification.ProviderId.Value);

            await _mediator.Send(command, cancellationToken);
        }
    }
}
