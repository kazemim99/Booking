// ========================================
// Booksy.Core.Application/Services/EmailTemplate.cs
// ========================================
namespace Booksy.Infrastructure.External.Notifications
{
    /// <summary>
    /// Represents an email template
    /// </summary>
    public sealed class EmailTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string? PlainTextBody { get; set; }
        public Dictionary<string, string> DefaultValues { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// Renders the template with the provided values
        /// </summary>
        public (string Subject, string HtmlBody, string? PlainTextBody) Render(Dictionary<string, string> values)
        {
            var mergedValues = new Dictionary<string, string>(DefaultValues);
            foreach (var kvp in values)
            {
                mergedValues[kvp.Key] = kvp.Value;
            }

            var renderedSubject = ReplaceTokens(Subject, mergedValues);
            var renderedHtml = ReplaceTokens(HtmlBody, mergedValues);
            var renderedPlainText = PlainTextBody != null
                ? ReplaceTokens(PlainTextBody, mergedValues)
                : null;

            return (renderedSubject, renderedHtml, renderedPlainText);
        }

        private static string ReplaceTokens(string template, Dictionary<string, string> values)
        {
            var result = template;
            foreach (var kvp in values)
            {
                result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }
            return result;
        }

        // Common email templates
        public static class Templates
        {
            public const string Welcome = "WELCOME";
            public const string EmailVerification = "EMAIL_VERIFICATION";
            public const string PasswordReset = "PASSWORD_RESET";
            public const string AppointmentConfirmation = "APPOINTMENT_CONFIRMATION";
            public const string AppointmentReminder = "APPOINTMENT_REMINDER";
            public const string AppointmentCancellation = "APPOINTMENT_CANCELLATION";
            public const string ReviewRequest = "REVIEW_REQUEST";
            public const string InvoiceNotification = "INVOICE_NOTIFICATION";
        }
    }
}
