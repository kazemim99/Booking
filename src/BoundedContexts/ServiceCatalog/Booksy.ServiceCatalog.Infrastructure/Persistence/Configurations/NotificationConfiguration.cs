// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for Notification aggregate
    /// </summary>
    public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications", "ServiceCatalog");

            builder.HasKey(n => n.Id);

            // Configure ID
            builder.Property(n => n.Id)
                .HasConversion(
                    id => id.Value,
                    value => NotificationId.From(value))
                .HasColumnName("NotificationId")
                .IsRequired();

            // Configure RecipientId
            builder.Property(n => n.RecipientId)
                .HasConversion(
                    id => id.Value,
                    value => Core.Domain.ValueObjects.UserId.From(value))
                .HasColumnName("RecipientId")
                .IsRequired();

            // Recipient Information
            builder.Property(n => n.RecipientEmail)
                .HasMaxLength(255);

            builder.Property(n => n.RecipientPhone)
                .HasMaxLength(50);

            builder.Property(n => n.RecipientName)
                .HasMaxLength(255);

            // Notification Content
            builder.Property(n => n.Type)
                .HasConversion<string>()
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(n => n.Channel)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(n => n.Priority)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(n => n.Subject)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(n => n.Body)
                .HasColumnType("text")
                .IsRequired();

            builder.Property(n => n.PlainTextBody)
                .HasColumnType("text");

            builder.Property(n => n.TemplateId)
                .HasMaxLength(100);

            // Template Data as JSON
            builder.Property(n => n.TemplateData)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

            // Status & Lifecycle
            builder.Property(n => n.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(n => n.CreatedAt)
                .IsRequired();

            builder.Property(n => n.ScheduledFor);
            builder.Property(n => n.SentAt);
            builder.Property(n => n.DeliveredAt);
            builder.Property(n => n.ReadAt);
            builder.Property(n => n.ExpiresAt);

            // Delivery Tracking
            builder.Property(n => n.AttemptCount)
                .IsRequired();

            builder.Property(n => n.GatewayMessageId)
                .HasMaxLength(255);

            builder.Property(n => n.ErrorMessage)
                .HasMaxLength(1000);

            // Related Entities
            builder.Property(n => n.BookingId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? BookingId.From(value.Value) : null);

            builder.Property(n => n.PaymentId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? PaymentId.From(value.Value) : null);

            builder.Property(n => n.ProviderId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? ProviderId.From(value.Value) : null);

            // Metadata as JSON
            builder.Property(n => n.Metadata)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

            builder.Property(n => n.CampaignId)
                .HasMaxLength(100);

            builder.Property(n => n.BatchId)
                .HasMaxLength(100);

            // Tracking
            builder.Property(n => n.OpenedFrom)
                .HasMaxLength(500);

            builder.Property(n => n.ClickedLink)
                .HasMaxLength(1000);

            builder.Property(n => n.OpenCount)
                .IsRequired();

            builder.Property(n => n.ClickCount)
                .IsRequired();

            // Configure owned collection - DeliveryAttempts
            builder.OwnsMany(n => n.DeliveryAttempts, attempts =>
            {
                attempts.ToTable("NotificationDeliveryAttempts", "ServiceCatalog");

                attempts.WithOwner()
                    .HasForeignKey("NotificationId");

                attempts.HasKey("Id");

                attempts.Property(a => a.AttemptNumber)
                    .IsRequired();

                attempts.Property(a => a.AttemptedAt)
                    .IsRequired();

                attempts.Property(a => a.Channel)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                attempts.Property(a => a.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                attempts.Property(a => a.GatewayMessageId)
                    .HasMaxLength(255);

                attempts.Property(a => a.ErrorMessage)
                    .HasMaxLength(1000);

                attempts.Property(a => a.ErrorCode)
                    .HasMaxLength(100);

                attempts.Property(a => a.HttpStatusCode);

                attempts.Property(a => a.Metadata)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

                attempts.Property(a => a.NextRetryAt);

                attempts.Property(a => a.RetryDelaySeconds)
                    .IsRequired();
            });

            // Indexes
            builder.HasIndex(n => n.RecipientId)
                .HasDatabaseName("IX_Notifications_RecipientId");

            builder.HasIndex(n => n.Status)
                .HasDatabaseName("IX_Notifications_Status");

            builder.HasIndex(n => n.Type)
                .HasDatabaseName("IX_Notifications_Type");

            builder.HasIndex(n => n.Channel)
                .HasDatabaseName("IX_Notifications_Channel");

            builder.HasIndex(n => n.CreatedAt)
                .HasDatabaseName("IX_Notifications_CreatedAt");

            builder.HasIndex(n => n.ScheduledFor)
                .HasDatabaseName("IX_Notifications_ScheduledFor")
                .HasFilter("\"ScheduledFor\" IS NOT NULL");

            builder.HasIndex(n => new { n.Status, n.ScheduledFor })
                .HasDatabaseName("IX_Notifications_Status_ScheduledFor");

            builder.HasIndex(n => new { n.BookingId, n.Type })
                .HasDatabaseName("IX_Notifications_BookingId_Type")
                .HasFilter("\"BookingId\" IS NOT NULL");
        }
    }
}
