using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>What a recipient actually reads.</summary>
    /// <param name="Subject">Title — the push heading and the inbox row's first line.</param>
    /// <param name="Body">The message.</param>
    /// <param name="PlainTextBody">The SMS form: no markup, short, and complete on its own.</param>
    public sealed record NotificationCopy(string Subject, string Body, string? PlainTextBody);

    /// <summary>
    /// Turns a notification and its captured parameters into the words for it.
    /// </summary>
    /// <remarks>
    /// Separate from raising and from sending so the wording can be reviewed and tested on its own, and so a
    /// notification's text is decided once rather than assembled differently by each handler that sends one.
    /// </remarks>
    public interface INotificationCopyWriter
    {
        /// <summary>
        /// Writes <paramref name="code"/> using <paramref name="parameters"/> captured when the event happened.
        /// </summary>
        NotificationCopy Write(NotificationEventCode code, IReadOnlyDictionary<string, string> parameters);
    }

    /// <summary>The parameter names the copy writer understands, so raise sites and copy cannot drift apart.</summary>
    public static class NotificationParameter
    {
        public const string CustomerName = "customerName";
        public const string BusinessName = "businessName";
        public const string ServiceName = "serviceName";
        public const string StaffName = "staffName";

        /// <summary>Round-trip ("o") format. A salon wall-clock value — see FOLLOW-UPS #63.</summary>
        public const string StartTime = "startTime";

        public const string Amount = "amount";
        public const string Reason = "reason";
        public const string Code = "code";
        public const string Count = "count";
    }
}
