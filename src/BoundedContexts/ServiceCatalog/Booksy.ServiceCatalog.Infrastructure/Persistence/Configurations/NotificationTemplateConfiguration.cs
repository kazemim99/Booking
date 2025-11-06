// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/NotificationTemplateConfiguration.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationTemplateAggregate;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for NotificationTemplate aggregate
    /// </summary>
    public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
    {
        public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
        {
            builder.ToTable("NotificationTemplates", "ServiceCatalog");

            builder.HasKey(t => t.Id);

            // Configure ID
            builder.Property(t => t.Id)
                .HasConversion(
                    id => id.Value,
                    value => TemplateId.From(value))
                .HasColumnName("TemplateId")
                .IsRequired();

            // Identity & Metadata
            builder.Property(t => t.TemplateKey)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(t => t.TemplateKey)
                .HasDatabaseName("IX_NotificationTemplates_TemplateKey");

            builder.Property(t => t.Name)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(1000);

            // Type & Channel
            builder.Property(t => t.Type)
                .HasConversion<string>()
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.SupportedChannels)
                .HasConversion<string>()
                .HasMaxLength(255)
                .IsRequired();

            // Email Templates
            builder.Property(t => t.EmailSubjectTemplate)
                .HasMaxLength(500);

            builder.Property(t => t.EmailBodyTemplate)
                .HasColumnType("text");

            builder.Property(t => t.EmailPlainTextTemplate)
                .HasColumnType("text");

            // SMS Template
            builder.Property(t => t.SmsBodyTemplate)
                .HasMaxLength(1600); // SMS with concatenation

            // Push Notification Templates
            builder.Property(t => t.PushTitleTemplate)
                .HasMaxLength(100);

            builder.Property(t => t.PushBodyTemplate)
                .HasMaxLength(500);

            // In-App Templates
            builder.Property(t => t.InAppTitleTemplate)
                .HasMaxLength(200);

            builder.Property(t => t.InAppBodyTemplate)
                .HasMaxLength(2000);

            // Versioning & Status
            builder.Property(t => t.TemplateVersion)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(t => t.IsActive)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(t => t.IsDraft)
                .IsRequired()
                .HasDefaultValue(true);

            // Variables as JSON arrays
            builder.Property(t => t.RequiredVariables)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .IsRequired();

            builder.Property(t => t.OptionalVariables)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .IsRequired();

            builder.Property(t => t.DefaultLanguage)
                .HasMaxLength(10)
                .HasDefaultValue("en");

            // Localized Versions as JSON dictionary
            builder.Property(t => t.LocalizedVersions)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
                .IsRequired();

            // Timestamps
            builder.Property(t => t.CreatedAt)
                .IsRequired();

            builder.Property(t => t.UpdatedAt);
            builder.Property(t => t.PublishedAt);
            builder.Property(t => t.ArchivedAt);

            // Tracking
            builder.Property(t => t.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(t => t.LastUsedAt);

            // Ignore domain events (not persisted)
            builder.Ignore(t => t.DomainEvents);

            // Indexes
            builder.HasIndex(t => new { t.Type, t.IsActive })
                .HasDatabaseName("IX_NotificationTemplates_Type_Active");

            builder.HasIndex(t => t.IsActive)
                .HasDatabaseName("IX_NotificationTemplates_IsActive");

            builder.HasIndex(t => new { t.TemplateKey, t.TemplateVersion })
                .HasDatabaseName("IX_NotificationTemplates_Key_Version");
        }
    }
}
