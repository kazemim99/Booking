// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/NotificationTemplateAggregate/NotificationTemplate.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.NotificationTemplateAggregate
{
    /// <summary>
    /// NotificationTemplate aggregate root with versioning support
    /// </summary>
    public sealed class NotificationTemplate : AggregateRoot<TemplateId>
    {
        // Identity
        public string TemplateKey { get; private set; } // Unique key like "booking-confirmation"
        public string Name { get; private set; }
        public string Description { get; private set; }

        // Type & Channel
        public NotificationType Type { get; private set; }
        public NotificationChannel SupportedChannels { get; private set; }

        // Content (with placeholders like {{customerName}}, {{bookingTime}})
        public string? EmailSubjectTemplate { get; private set; }
        public string? EmailBodyTemplate { get; private set; }
        public string? EmailPlainTextTemplate { get; private set; }
        public string? SmsBodyTemplate { get; private set; }
        public string? PushTitleTemplate { get; private set; }
        public string? PushBodyTemplate { get; private set; }
        public string? InAppTitleTemplate { get; private set; }
        public string? InAppBodyTemplate { get; private set; }

        // Versioning
        public int TemplateVersion { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDraft { get; private set; }

        // Metadata
        public List<string> RequiredVariables { get; private set; }
        public List<string> OptionalVariables { get; private set; }
        public string? DefaultLanguage { get; private set; }
        public Dictionary<string, string> LocalizedVersions { get; private set; } // languageCode -> templateId

        // Timestamps
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? PublishedAt { get; private set; }
        public DateTime? ArchivedAt { get; private set; }

        // Tracking
        public int UsageCount { get; private set; }
        public DateTime? LastUsedAt { get; private set; }

        // EF Core constructor
        private NotificationTemplate() : base(TemplateId.Create())
        {
            TemplateKey = string.Empty;
            Name = string.Empty;
            Description = string.Empty;
            RequiredVariables = new List<string>();
            OptionalVariables = new List<string>();
            LocalizedVersions = new Dictionary<string, string>();
        }

        private NotificationTemplate(
            string templateKey,
            string name,
            string description,
            NotificationType type,
            NotificationChannel supportedChannels) : base(TemplateId.Create())
        {
            TemplateKey = templateKey;
            Name = name;
            Description = description;
            Type = type;
            SupportedChannels = supportedChannels;
            TemplateVersion = 1;
            IsActive = false;
            IsDraft = true;
            CreatedAt = DateTime.UtcNow;
            RequiredVariables = new List<string>();
            OptionalVariables = new List<string>();
            LocalizedVersions = new Dictionary<string, string>();
            DefaultLanguage = "en";
        }

        public static NotificationTemplate Create(
            string templateKey,
            string name,
            string description,
            NotificationType type,
            NotificationChannel supportedChannels)
        {
            if (string.IsNullOrWhiteSpace(templateKey))
                throw new ArgumentException("Template key cannot be empty", nameof(templateKey));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            return new NotificationTemplate(templateKey, name, description, type, supportedChannels);
        }

        public void SetEmailTemplate(string subject, string bodyHtml, string? plainText = null)
        {
            if (!SupportedChannels.HasFlag(NotificationChannel.Email))
                throw new InvalidOperationException("Email channel is not supported for this template");

            EmailSubjectTemplate = subject;
            EmailBodyTemplate = bodyHtml;
            EmailPlainTextTemplate = plainText;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetSmsTemplate(string body)
        {
            if (!SupportedChannels.HasFlag(NotificationChannel.SMS))
                throw new InvalidOperationException("SMS channel is not supported for this template");

            if (body.Length > 1600) // SMS limit with concatenation
                throw new ArgumentException("SMS template body is too long (max 1600 characters)", nameof(body));

            SmsBodyTemplate = body;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPushTemplate(string title, string body)
        {
            if (!SupportedChannels.HasFlag(NotificationChannel.PushNotification))
                throw new InvalidOperationException("Push notification channel is not supported for this template");

            PushTitleTemplate = title;
            PushBodyTemplate = body;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetInAppTemplate(string title, string body)
        {
            if (!SupportedChannels.HasFlag(NotificationChannel.InApp))
                throw new InvalidOperationException("In-app channel is not supported for this template");

            InAppTitleTemplate = title;
            InAppBodyTemplate = body;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetVariables(List<string> required, List<string> optional)
        {
            RequiredVariables = required ?? new List<string>();
            OptionalVariables = optional ?? new List<string>();
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddLocalizedVersion(string languageCode, string localizedTemplateId)
        {
            LocalizedVersions[languageCode] = localizedTemplateId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Publish()
        {
            // Validate that at least one channel template is set
            bool hasTemplate = (SupportedChannels.HasFlag(NotificationChannel.Email) && !string.IsNullOrEmpty(EmailBodyTemplate)) ||
                               (SupportedChannels.HasFlag(NotificationChannel.SMS) && !string.IsNullOrEmpty(SmsBodyTemplate)) ||
                               (SupportedChannels.HasFlag(NotificationChannel.PushNotification) && !string.IsNullOrEmpty(PushBodyTemplate)) ||
                               (SupportedChannels.HasFlag(NotificationChannel.InApp) && !string.IsNullOrEmpty(InAppBodyTemplate));

            if (!hasTemplate)
                throw new InvalidOperationException("Cannot publish template without any channel content");

            IsActive = true;
            IsDraft = false;
            PublishedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Archive()
        {
            IsActive = false;
            ArchivedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void IncrementVersion()
        {
            TemplateVersion++;
            IsDraft = true;
            IsActive = false;
            PublishedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void RecordUsage()
        {
            UsageCount++;
            LastUsedAt = DateTime.UtcNow;
        }

        public bool SupportsChannel(NotificationChannel channel)
        {
            return SupportedChannels.HasFlag(channel);
        }

        public string? GetTemplateForChannel(NotificationChannel channel)
        {
            return channel switch
            {
                NotificationChannel.Email => EmailBodyTemplate,
                NotificationChannel.SMS => SmsBodyTemplate,
                NotificationChannel.PushNotification => PushBodyTemplate,
                NotificationChannel.InApp => InAppBodyTemplate,
                _ => null
            };
        }
    }
}
