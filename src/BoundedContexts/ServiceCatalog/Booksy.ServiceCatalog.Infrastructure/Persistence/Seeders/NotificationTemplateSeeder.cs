// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/NotificationTemplateSeeder.cs
// ========================================
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationTemplateAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds notification templates for booking and payment scenarios
    /// </summary>
    public sealed class NotificationTemplateSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<NotificationTemplateSeeder> _logger;

        public NotificationTemplateSeeder(
            ServiceCatalogDbContext context,
            ILogger<NotificationTemplateSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if templates already exist
                if (await _context.NotificationTemplates.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Notification templates already seeded. Skipping...");
                    return;
                }

                var templates = new List<NotificationTemplate>
                {
                    // ========== BOOKING TEMPLATES ==========
                    CreateBookingConfirmationTemplate(),
                    CreateBookingReminderTemplate(),
                    CreateBookingCancellationTemplate(),
                    CreateBookingRescheduledTemplate(),
                    CreateBookingNoShowTemplate(),
                    CreateBookingReviewRequestTemplate(),

                    // ========== PAYMENT TEMPLATES ==========
                    CreatePaymentSuccessTemplate(),
                    CreatePaymentFailedTemplate(),
                    CreateRefundProcessedTemplate(),
                    CreateInvoiceGeneratedTemplate(),
                    CreatePayoutCompletedTemplate(),

                    // ========== ACCOUNT TEMPLATES ==========
                    CreateWelcomeEmailTemplate(),
                    CreateEmailVerificationTemplate(),
                    CreatePasswordResetTemplate(),
                    CreatePhoneVerificationTemplate(),
                    CreateAccountDeactivatedTemplate()
                };

                // Publish all templates
                foreach (var template in templates)
                {
                    template.Publish();
                }

                await _context.NotificationTemplates.AddRangeAsync(templates, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully seeded {Count} notification templates", templates.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding notification templates");
                throw;
            }
        }

        #region Booking Templates

        private NotificationTemplate CreateBookingConfirmationTemplate()
        {
            var template = NotificationTemplate.Create(
                "booking-confirmation",
                "Booking Confirmation",
                "Sent to customer when a booking is confirmed",
                NotificationType.BookingConfirmed,
                NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.PushNotification);

            // Email template
            template.SetEmailTemplate(
                subject: "Booking Confirmed - {{ServiceName}} on {{BookingDate:date}}",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #4CAF50; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f9f9f9; }
        .details { background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #4CAF50; }
        .footer { text-align: center; padding: 20px; font-size: 12px; color: #777; }
        .button { background-color: #4CAF50; color: white; padding: 12px 24px; text-decoration: none; display: inline-block; margin: 10px 0; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Booking Confirmed!</h1>
        </div>
        <div class=""content"">
            <p>Hi {{CustomerName}},</p>
            <p>Your booking has been confirmed. We're looking forward to seeing you!</p>

            <div class=""details"">
                <h3>Booking Details</h3>
                <p><strong>Service:</strong> {{ServiceName}}</p>
                <p><strong>Provider:</strong> {{ProviderName}}</p>
                <p><strong>Date:</strong> {{BookingDate:longdate}}</p>
                <p><strong>Time:</strong> {{BookingTime:time}}</p>
                <p><strong>Duration:</strong> {{Duration}} minutes</p>
                <p><strong>Price:</strong> {{Price:currency}}</p>
                <p><strong>Booking Reference:</strong> {{BookingReference}}</p>
            </div>

            <div class=""details"">
                <h3>Location</h3>
                <p>{{ProviderAddress}}</p>
            </div>

            <p>If you need to reschedule or cancel, please do so at least 24 hours in advance.</p>
            <a href=""{{ManageBookingUrl}}"" class=""button"">Manage Booking</a>
        </div>
        <div class=""footer"">
            <p>Thank you for choosing Booksy!</p>
            <p>Questions? Contact us at support@booksy.com</p>
        </div>
    </div>
</body>
</html>",
                plainText: @"Booking Confirmed!

Hi {{CustomerName}},

Your booking has been confirmed. We're looking forward to seeing you!

Booking Details:
- Service: {{ServiceName}}
- Provider: {{ProviderName}}
- Date: {{BookingDate:longdate}}
- Time: {{BookingTime:time}}
- Duration: {{Duration}} minutes
- Price: {{Price:currency}}
- Reference: {{BookingReference}}

Location:
{{ProviderAddress}}

Manage your booking: {{ManageBookingUrl}}

Thank you for choosing Booksy!");

            // SMS template
            template.SetSmsTemplate(
                "Booking confirmed! {{ServiceName}} with {{ProviderName}} on {{BookingDate:date}} at {{BookingTime:time}}. Ref: {{BookingReference}}. Details: {{ManageBookingUrl}}");

            // Push notification
            template.SetPushTemplate(
                title: "Booking Confirmed",
                body: "{{ServiceName}} on {{BookingDate:date}} at {{BookingTime:time}}");

            template.SetVariables(
                required: new List<string> { "CustomerName", "ServiceName", "ProviderName", "BookingDate", "BookingTime", "Duration", "Price", "BookingReference", "ProviderAddress", "ManageBookingUrl" },
                optional: new List<string> { "ProviderPhone", "ProviderEmail", "SpecialInstructions" });

            return template;
        }

        private NotificationTemplate CreateBookingReminderTemplate()
        {
            var template = NotificationTemplate.Create(
                "booking-reminder",
                "Booking Reminder",
                "Sent 24 hours and 2 hours before booking",
                NotificationType.BookingReminder,
                NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.PushNotification);

            template.SetEmailTemplate(
                subject: "Reminder: {{ServiceName}} tomorrow at {{BookingTime:time}}",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #2196F3;"">Upcoming Booking Reminder</h2>
        <p>Hi {{CustomerName}},</p>
        <p>This is a friendly reminder about your upcoming booking:</p>

        <div style=""background-color: #f0f8ff; padding: 15px; margin: 15px 0; border-left: 4px solid #2196F3;"">
            <p><strong>Service:</strong> {{ServiceName}}</p>
            <p><strong>Provider:</strong> {{ProviderName}}</p>
            <p><strong>Date:</strong> {{BookingDate:longdate}}</p>
            <p><strong>Time:</strong> {{BookingTime:time}}</p>
            <p><strong>Location:</strong> {{ProviderAddress}}</p>
        </div>

        <p>Please arrive 5-10 minutes early. If you need to cancel or reschedule, please contact us as soon as possible.</p>
        <p>See you soon!</p>
    </div>
</body>
</html>");

            template.SetSmsTemplate(
                "Reminder: {{ServiceName}} with {{ProviderName}} {{ReminderTime}}. Location: {{ProviderAddress}}. Questions? Call {{ProviderPhone}}");

            template.SetPushTemplate(
                title: "Upcoming Booking Reminder",
                body: "{{ServiceName}} {{ReminderTime}} at {{ProviderName}}");

            template.SetVariables(
                required: new List<string> { "CustomerName", "ServiceName", "ProviderName", "BookingDate", "BookingTime", "ProviderAddress", "ReminderTime" },
                optional: new List<string> { "ProviderPhone" });

            return template;
        }

        private NotificationTemplate CreateBookingCancellationTemplate()
        {
            var template = NotificationTemplate.Create(
                "booking-cancellation",
                "Booking Cancellation",
                "Sent when a booking is cancelled",
                NotificationType.BookingCancelled,
                NotificationChannel.Email | NotificationChannel.SMS);

            template.SetEmailTemplate(
                subject: "Booking Cancelled - {{ServiceName}}",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #f44336;"">Booking Cancelled</h2>
        <p>Hi {{CustomerName}},</p>
        <p>Your booking has been cancelled as requested.</p>

        <div style=""background-color: #ffebee; padding: 15px; margin: 15px 0; border-left: 4px solid #f44336;"">
            <p><strong>Cancelled Booking:</strong></p>
            <p>Service: {{ServiceName}}</p>
            <p>Date: {{BookingDate:longdate}} at {{BookingTime:time}}</p>
            <p>Reference: {{BookingReference}}</p>
        </div>

        {{#RefundMessage}}
        <p>{{RefundMessage}}</p>
        {{/RefundMessage}}

        <p>We hope to see you again soon! Book another appointment: {{BookingUrl}}</p>
    </div>
</body>
</html>");

            template.SetSmsTemplate(
                "Booking cancelled: {{ServiceName}} on {{BookingDate:date}}. Ref: {{BookingReference}}. {{RefundMessage}}");

            template.SetVariables(
                required: new List<string> { "CustomerName", "ServiceName", "BookingDate", "BookingTime", "BookingReference" },
                optional: new List<string> { "RefundMessage", "BookingUrl", "CancellationReason" });

            return template;
        }

        private NotificationTemplate CreateBookingRescheduledTemplate()
        {
            var template = NotificationTemplate.Create(
                "booking-rescheduled",
                "Booking Rescheduled",
                "Sent when a booking is rescheduled",
                NotificationType.BookingUpdated,
                NotificationChannel.Email | NotificationChannel.SMS);

            template.SetEmailTemplate(
                subject: "Booking Rescheduled - {{ServiceName}}",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #FF9800;"">Booking Rescheduled</h2>
        <p>Hi {{CustomerName}},</p>
        <p>Your booking has been successfully rescheduled.</p>

        <div style=""background-color: #fff3e0; padding: 15px; margin: 15px 0; border-left: 4px solid #FF9800;"">
            <p><strong>New Booking Details:</strong></p>
            <p>Service: {{ServiceName}}</p>
            <p>Date: {{NewBookingDate:longdate}}</p>
            <p>Time: {{NewBookingTime:time}}</p>
            <p>Provider: {{ProviderName}}</p>
            <p>Location: {{ProviderAddress}}</p>
        </div>

        <p><small><em>Previous date was: {{OldBookingDate:longdate}} at {{OldBookingTime:time}}</em></small></p>

        <p>See you at the new time!</p>
    </div>
</body>
</html>");

            template.SetSmsTemplate(
                "Booking rescheduled: {{ServiceName}} moved to {{NewBookingDate:date}} at {{NewBookingTime:time}}. Ref: {{BookingReference}}");

            template.SetVariables(
                required: new List<string> { "CustomerName", "ServiceName", "NewBookingDate", "NewBookingTime", "ProviderName", "ProviderAddress", "BookingReference" },
                optional: new List<string> { "OldBookingDate", "OldBookingTime" });

            return template;
        }

        private NotificationTemplate CreateBookingNoShowTemplate()
        {
            var template = NotificationTemplate.Create(
                "booking-no-show",
                "No-Show Alert",
                "Sent when customer doesn't show up for booking",
                NotificationType.BookingNoShow,
                NotificationChannel.Email);

            template.SetEmailTemplate(
                subject: "We missed you - {{ServiceName}}",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2>We Missed You!</h2>
        <p>Hi {{CustomerName}},</p>
        <p>We noticed you weren't able to make it to your appointment today.</p>

        <div style=""background-color: #f5f5f5; padding: 15px; margin: 15px 0;"">
            <p><strong>Missed Appointment:</strong></p>
            <p>Service: {{ServiceName}}</p>
            <p>Date: {{BookingDate:longdate}} at {{BookingTime:time}}</p>
        </div>

        <p>If you'd like to reschedule, we'd be happy to help. Please book online or contact us directly.</p>
        <p>Please note: {{NoShowPolicy}}</p>
    </div>
</body>
</html>");

            template.SetVariables(
                required: new List<string> { "CustomerName", "ServiceName", "BookingDate", "BookingTime", "NoShowPolicy" },
                optional: new List<string> { "RescheduleLock", "ProviderContact" });

            return template;
        }

        private NotificationTemplate CreateBookingReviewRequestTemplate()
        {
            var template = NotificationTemplate.Create(
                "booking-review-request",
                "Review Request",
                "Sent after booking completion to request review",
                NotificationType.ReviewRequest,
                NotificationChannel.Email | NotificationChannel.InApp);

            template.SetEmailTemplate(
                subject: "How was your {{ServiceName}} experience?",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #4CAF50;"">We'd Love Your Feedback!</h2>
        <p>Hi {{CustomerName}},</p>
        <p>Thank you for choosing {{ProviderName}} for your {{ServiceName}}!</p>

        <p>We hope you had a great experience. Would you mind taking a moment to share your thoughts?</p>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{{ReviewUrl}}"" style=""background-color: #4CAF50; color: white; padding: 12px 30px; text-decoration: none; display: inline-block; border-radius: 5px;"">Leave a Review</a>
        </div>

        <p style=""font-size: 12px; color: #777;"">Your feedback helps us improve and helps others find great service providers like {{ProviderName}}.</p>
    </div>
</body>
</html>");

            template.SetVariables(
                required: new List<string> { "CustomerName", "ServiceName", "ProviderName", "ReviewUrl" },
                optional: new List<string> { "BookingDate", "Rating" });

            return template;
        }

        #endregion

        #region Payment Templates

        private NotificationTemplate CreatePaymentSuccessTemplate()
        {
            var template = NotificationTemplate.Create(
                "payment-success",
                "Payment Successful",
                "Sent when payment is successfully processed",
                NotificationType.PaymentConfirmed,
                NotificationChannel.Email | NotificationChannel.SMS);

            template.SetEmailTemplate(
                subject: "Payment Received - {{Amount:currency}}",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #4CAF50;"">Payment Successful</h2>
        <p>Hi {{CustomerName}},</p>
        <p>We've successfully received your payment. Thank you!</p>

        <div style=""background-color: #e8f5e9; padding: 15px; margin: 15px 0; border-left: 4px solid #4CAF50;"">
            <p><strong>Payment Details:</strong></p>
            <p>Amount: {{Amount:currency}}</p>
            <p>Service: {{ServiceName}}</p>
            <p>Transaction ID: {{TransactionId}}</p>
            <p>Date: {{PaymentDate:datetime}}</p>
            <p>Payment Method: {{PaymentMethod}}</p>
        </div>

        <p>Your receipt is attached to this email.</p>
        <p>View full details: <a href=""{{ReceiptUrl}}"">{{ReceiptUrl}}</a></p>
    </div>
</body>
</html>");

            template.SetSmsTemplate(
                "Payment of {{Amount:currency}} received for {{ServiceName}}. Transaction ID: {{TransactionId}}. View receipt: {{ReceiptUrl}}");

            template.SetVariables(
                required: new List<string> { "CustomerName", "Amount", "ServiceName", "TransactionId", "PaymentDate", "PaymentMethod", "ReceiptUrl" },
                optional: new List<string> { "InvoiceNumber", "TaxAmount" });

            return template;
        }

        private NotificationTemplate CreatePaymentFailedTemplate()
        {
            var template = NotificationTemplate.Create(
                "payment-failed",
                "Payment Failed",
                "Sent when payment processing fails",
                NotificationType.PaymentFailed,
                NotificationChannel.Email | NotificationChannel.SMS);

            template.SetEmailTemplate(
                subject: "Payment Failed - Action Required",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #f44336;"">Payment Failed</h2>
        <p>Hi {{CustomerName}},</p>
        <p>We were unable to process your payment for {{ServiceName}}.</p>

        <div style=""background-color: #ffebee; padding: 15px; margin: 15px 0; border-left: 4px solid #f44336;"">
            <p><strong>Payment Details:</strong></p>
            <p>Amount: {{Amount:currency}}</p>
            <p>Reason: {{FailureReason}}</p>
            <p>Date: {{PaymentDate:datetime}}</p>
        </div>

        <p>Please update your payment method and try again to avoid booking cancellation.</p>
        <a href=""{{RetryPaymentUrl}}"" style=""background-color: #f44336; color: white; padding: 12px 24px; text-decoration: none; display: inline-block; margin: 10px 0;"">Update Payment</a>
    </div>
</body>
</html>");

            template.SetSmsTemplate(
                "Payment failed for {{ServiceName}} ({{Amount:currency}}). Please update payment: {{RetryPaymentUrl}}");

            template.SetVariables(
                required: new List<string> { "CustomerName", "ServiceName", "Amount", "FailureReason", "PaymentDate", "RetryPaymentUrl" },
                optional: new List<string> { "BookingReference" });

            return template;
        }

        private NotificationTemplate CreateRefundProcessedTemplate()
        {
            var template = NotificationTemplate.Create(
                "refund-processed",
                "Refund Processed",
                "Sent when refund is successfully processed",
                NotificationType.RefundIssued,
                NotificationChannel.Email);

            template.SetEmailTemplate(
                subject: "Refund Processed - {{Amount:currency}}",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #2196F3;"">Refund Processed</h2>
        <p>Hi {{CustomerName}},</p>
        <p>Your refund has been processed successfully.</p>

        <div style=""background-color: #e3f2fd; padding: 15px; margin: 15px 0; border-left: 4px solid #2196F3;"">
            <p><strong>Refund Details:</strong></p>
            <p>Amount: {{Amount:currency}}</p>
            <p>Original Payment: {{OriginalTransactionId}}</p>
            <p>Refund ID: {{RefundId}}</p>
            <p>Date: {{RefundDate:datetime}}</p>
        </div>

        <p>The refund will appear in your account within {{RefundDays}} business days.</p>
        <p>If you have any questions, please contact our support team.</p>
    </div>
</body>
</html>");

            template.SetVariables(
                required: new List<string> { "CustomerName", "Amount", "OriginalTransactionId", "RefundId", "RefundDate", "RefundDays" },
                optional: new List<string> { "RefundReason" });

            return template;
        }

        private NotificationTemplate CreateInvoiceGeneratedTemplate()
        {
            var template = NotificationTemplate.Create(
                "invoice-generated",
                "Invoice Generated",
                "Sent when invoice is generated for a booking",
                NotificationType.InvoiceGenerated,
                NotificationChannel.Email);

            template.SetEmailTemplate(
                subject: "Invoice #{{InvoiceNumber}} - {{ServiceName}}",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2>Invoice for {{ServiceName}}</h2>
        <p>Hi {{CustomerName}},</p>
        <p>Your invoice is ready. Please find the details below:</p>

        <div style=""background-color: #f5f5f5; padding: 15px; margin: 15px 0;"">
            <p><strong>Invoice #{{InvoiceNumber}}</strong></p>
            <p>Date: {{InvoiceDate:date}}</p>
            <p>Service: {{ServiceName}}</p>
            <p>Amount: {{Amount:currency}}</p>
            <p>Tax: {{TaxAmount:currency}}</p>
            <p><strong>Total: {{TotalAmount:currency}}</strong></p>
        </div>

        <p>Download your invoice: <a href=""{{InvoiceUrl}}"">{{InvoiceUrl}}</a></p>
    </div>
</body>
</html>");

            template.SetVariables(
                required: new List<string> { "CustomerName", "InvoiceNumber", "InvoiceDate", "ServiceName", "Amount", "TaxAmount", "TotalAmount", "InvoiceUrl" },
                optional: new List<string> { "PaymentDueDate" });

            return template;
        }

        private NotificationTemplate CreatePayoutCompletedTemplate()
        {
            var template = NotificationTemplate.Create(
                "payout-completed",
                "Payout Completed",
                "Sent to providers when payout is processed",
                NotificationType.PayoutProcessed,
                NotificationChannel.Email);

            template.SetEmailTemplate(
                subject: "Payout Processed - {{Amount:currency}}",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #4CAF50;"">Payout Processed</h2>
        <p>Hi {{ProviderName}},</p>
        <p>Your payout has been successfully processed and is on its way to your account.</p>

        <div style=""background-color: #e8f5e9; padding: 15px; margin: 15px 0; border-left: 4px solid #4CAF50;"">
            <p><strong>Payout Details:</strong></p>
            <p>Amount: {{Amount:currency}}</p>
            <p>Period: {{PeriodStart:date}} - {{PeriodEnd:date}}</p>
            <p>Payout ID: {{PayoutId}}</p>
            <p>Date: {{PayoutDate:datetime}}</p>
        </div>

        <p>The funds should arrive in your account within {{PayoutDays}} business days.</p>
        <p><a href=""{{PayoutDetailsUrl}}"">View Payout Details</a></p>
    </div>
</body>
</html>");

            template.SetVariables(
                required: new List<string> { "ProviderName", "Amount", "PeriodStart", "PeriodEnd", "PayoutId", "PayoutDate", "PayoutDays", "PayoutDetailsUrl" },
                optional: new List<string> { "BookingsCount", "Commission" });

            return template;
        }

        #endregion

        #region Account Templates

        private NotificationTemplate CreateWelcomeEmailTemplate()
        {
            var template = NotificationTemplate.Create(
                "welcome-email",
                "Welcome Email",
                "Sent to new users after registration",
                NotificationType.Welcome,
                NotificationChannel.Email);

            template.SetEmailTemplate(
                subject: "Welcome to Booksy, {{FirstName}}!",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h1 style=""color: #4CAF50; text-align: center;"">Welcome to Booksy!</h1>
        <p>Hi {{FirstName}},</p>
        <p>Thank you for joining Booksy! We're excited to have you on board.</p>

        <p>With Booksy, you can:</p>
        <ul>
            <li>Book appointments with top service providers</li>
            <li>Manage your bookings easily</li>
            <li>Discover new services in your area</li>
            <li>Get exclusive deals and offers</li>
        </ul>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{{ExploreUrl}}"" style=""background-color: #4CAF50; color: white; padding: 15px 30px; text-decoration: none; display: inline-block; border-radius: 5px;"">Start Exploring</a>
        </div>

        <p>If you have any questions, our support team is always here to help.</p>
        <p>Welcome aboard!</p>
    </div>
</body>
</html>");

            template.SetVariables(
                required: new List<string> { "FirstName", "ExploreUrl" },
                optional: new List<string> { "LastName", "Email" });

            return template;
        }

        private NotificationTemplate CreateEmailVerificationTemplate()
        {
            var template = NotificationTemplate.Create(
                "email-verification",
                "Email Verification",
                "Sent to verify user's email address",
                NotificationType.AccountVerification,
                NotificationChannel.Email);

            template.SetEmailTemplate(
                subject: "Verify your email address",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #2196F3;"">Verify Your Email</h2>
        <p>Hi {{FirstName}},</p>
        <p>Please verify your email address to activate your Booksy account.</p>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{{VerificationUrl}}"" style=""background-color: #2196F3; color: white; padding: 15px 30px; text-decoration: none; display: inline-block; border-radius: 5px;"">Verify Email</a>
        </div>

        <p>Or enter this code: <strong style=""font-size: 20px; color: #2196F3;"">{{VerificationCode}}</strong></p>
        <p><small>This link will expire in {{ExpirationMinutes}} minutes.</small></p>

        <p>If you didn't create this account, please ignore this email.</p>
    </div>
</body>
</html>");

            template.SetVariables(
                required: new List<string> { "FirstName", "VerificationUrl", "VerificationCode", "ExpirationMinutes" },
                optional: new List<string> { });

            return template;
        }

        private NotificationTemplate CreatePasswordResetTemplate()
        {
            var template = NotificationTemplate.Create(
                "password-reset",
                "Password Reset",
                "Sent when user requests password reset",
                NotificationType.PasswordReset,
                NotificationChannel.Email);

            template.SetEmailTemplate(
                subject: "Reset your Booksy password",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #FF9800;"">Password Reset Request</h2>
        <p>Hi {{FirstName}},</p>
        <p>You requested to reset your password. Click the button below to set a new password:</p>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{{ResetUrl}}"" style=""background-color: #FF9800; color: white; padding: 15px 30px; text-decoration: none; display: inline-block; border-radius: 5px;"">Reset Password</a>
        </div>

        <p><small>This link will expire at {{ExpiresAt:datetime}}.</small></p>

        <p>If you didn't request this, please ignore this email. Your password will remain unchanged.</p>
        <p style=""color: #777; font-size: 12px;"">For security, never share this link with anyone.</p>
    </div>
</body>
</html>");

            template.SetVariables(
                required: new List<string> { "FirstName", "ResetUrl", "ExpiresAt" },
                optional: new List<string> { });

            return template;
        }

        private NotificationTemplate CreatePhoneVerificationTemplate()
        {
            var template = NotificationTemplate.Create(
                "phone-verification-otp",
                "Phone Verification OTP",
                "Sent for phone number verification",
                NotificationType.PhoneVerification,
                NotificationChannel.SMS);

            template.SetSmsTemplate(
                "Your Booksy verification code is: {{OtpCode}}. Valid for {{ValidityMinutes}} minutes. Do not share this code.");

            template.SetVariables(
                required: new List<string> { "OtpCode", "ValidityMinutes" },
                optional: new List<string> { });

            return template;
        }

        private NotificationTemplate CreateAccountDeactivatedTemplate()
        {
            var template = NotificationTemplate.Create(
                "account-deactivated",
                "Account Deactivated",
                "Sent when user account is deactivated",
                NotificationType.AccountDeactivated,
                NotificationChannel.Email);

            template.SetEmailTemplate(
                subject: "Your Booksy account has been deactivated",
                bodyHtml: @"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2>Account Deactivated</h2>
        <p>Hi {{FirstName}},</p>
        <p>Your Booksy account has been deactivated as requested.</p>

        <p>{{#DeactivationReason}}Reason: {{DeactivationReason}}{{/DeactivationReason}}</p>

        <p>If you change your mind, you can reactivate your account anytime by logging in within the next {{ReactivationDays}} days.</p>

        <p>After {{ReactivationDays}} days, your account and all associated data will be permanently deleted.</p>

        <p>We're sorry to see you go. If there's anything we could have done better, please let us know.</p>
    </div>
</body>
</html>");

            template.SetVariables(
                required: new List<string> { "FirstName", "ReactivationDays" },
                optional: new List<string> { "DeactivationReason" });

            return template;
        }

        #endregion
    }
}
