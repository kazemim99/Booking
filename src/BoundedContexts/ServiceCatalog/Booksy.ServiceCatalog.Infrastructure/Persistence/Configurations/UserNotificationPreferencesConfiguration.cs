// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/UserNotificationPreferencesConfiguration.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for UserNotificationPreferences aggregate
    /// </summary>
    public sealed class UserNotificationPreferencesConfiguration : IEntityTypeConfiguration<UserNotificationPreferences>
    {
        public void Configure(EntityTypeBuilder<UserNotificationPreferences> builder)
        {
            builder.ToTable("UserNotificationPreferences", "ServiceCatalog");

            builder.HasKey(p => p.Id);

            // Configure ID (UserId)
            builder.Property(p => p.Id)
                .HasConversion(
                    id => id.Value,
                    value => UserId.From(value))
                .HasColumnName("UserId")
                .IsRequired();

            // Configure NotificationPreference as owned entity
            builder.OwnsOne(p => p.Preferences, prefs =>
            {
                prefs.Property(p => p.EnabledChannels)
                    .HasConversion<string>()
                    .HasColumnName("EnabledChannels")
                    .HasMaxLength(255)
                    .IsRequired();

                prefs.Property(p => p.EnabledTypes)
                    .HasConversion<string>()
                    .HasColumnName("EnabledTypes")
                    .HasMaxLength(1000)
                    .IsRequired();

                prefs.Property(p => p.QuietHoursStart)
                    .HasColumnName("QuietHoursStart");

                prefs.Property(p => p.QuietHoursEnd)
                    .HasColumnName("QuietHoursEnd");

                prefs.Property(p => p.PreferredLanguage)
                    .HasColumnName("PreferredLanguage")
                    .HasMaxLength(10)
                    .HasDefaultValue("en");

                prefs.Property(p => p.MarketingOptIn)
                    .HasColumnName("MarketingOptIn")
                    .IsRequired()
                    .HasDefaultValue(false);

                prefs.Property(p => p.MaxNotificationsPerDay)
                    .HasColumnName("MaxNotificationsPerDay")
                    .IsRequired()
                    .HasDefaultValue(50);
            });

            // Timestamps
            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.LastUpdated)
                .IsRequired();

            // Ignore domain events (not persisted)
            builder.Ignore(p => p.DomainEvents);

            // Indexes
            builder.HasIndex(p => p.Id)
                .HasDatabaseName("IX_UserNotificationPreferences_UserId")
                .IsUnique();

            builder.HasIndex(p => p.LastUpdated)
                .HasDatabaseName("IX_UserNotificationPreferences_LastUpdated");
        }
    }
}
