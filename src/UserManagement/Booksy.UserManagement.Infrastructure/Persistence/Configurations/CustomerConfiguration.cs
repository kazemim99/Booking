// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Booksy.UserManagement.Domain.Aggregates.CustomerAggregate;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Infrastructure.Persistence.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("customers", "user_management");

            // Primary Key
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .HasConversion(
                    id => id.Value,
                    value => CustomerId.From(value))
                .HasColumnName("id");

            // Concurrency token
            builder.Property(c => c.Version)
                .HasColumnName("version")
                .IsConcurrencyToken()
                .HasDefaultValue(0)
                .IsRequired();

            // UserId (FK to User)
            builder.Property(c => c.UserId)
                .HasConversion(
                    id => id.Value,
                    value => UserId.From(value))
                .HasColumnName("user_id")
                .IsRequired();

            builder.HasIndex(c => c.UserId)
                .IsUnique()
                .HasDatabaseName("ix_customers_user_id");

            // Profile relationship - Customer accesses Profile through User, not directly
            // The Profile entity is owned by User aggregate, so we ignore it here
            builder.Ignore(c => c.Profile);

            // NotificationPreferences (owned value object)
            builder.OwnsOne(c => c.NotificationPreferences, np =>
            {
                np.Property(p => p.SmsEnabled)
                    .HasColumnName("notification_sms_enabled")
                    .IsRequired()
                    .HasDefaultValue(true);

                np.Property(p => p.EmailEnabled)
                    .HasColumnName("notification_email_enabled")
                    .IsRequired()
                    .HasDefaultValue(true);

                np.Property(p => p.ReminderTiming)
                    .HasColumnName("notification_reminder_timing")
                    .HasMaxLength(10)
                    .IsRequired()
                    .HasDefaultValue("24h");
            });

            // Status
            builder.Property(c => c.IsActive)
                .HasColumnName("is_active")
                .IsRequired()
                .HasDefaultValue(true);

            // Audit fields
            builder.Property(c => c.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(c => c.CreatedBy)
                .HasColumnName("created_by")
                .HasMaxLength(256);

            builder.Property(c => c.LastModifiedAt)
                .HasColumnName("last_modified_at");

            builder.Property(c => c.LastModifiedBy)
                .HasColumnName("last_modified_by")
                .HasMaxLength(256);

            // Favorite Providers collection
            builder.OwnsMany(c => c.FavoriteProviders, fp =>
            {
                fp.ToTable("customer_favorite_providers", "user_management");

                fp.WithOwner().HasForeignKey("CustomerId");

                fp.HasKey(f => f.Id);

                fp.Property(f => f.Id)
                    .HasColumnName("id");

                fp.Property(f => f.ProviderId)
                    .HasColumnName("provider_id")
                    .IsRequired();

                fp.Property(f => f.AddedAt)
                    .HasColumnName("added_at")
                    .IsRequired();

                fp.Property(f => f.Notes)
                    .HasColumnName("notes")
                    .HasMaxLength(500);

                fp.HasIndex(f => new { f.ProviderId })
                    .HasDatabaseName("ix_customer_favorite_providers_provider_id");
            });

            // Indexes
            builder.HasIndex(c => c.CreatedAt)
                .HasDatabaseName("ix_customers_created_at");

            builder.HasIndex(c => c.IsActive)
                .HasDatabaseName("ix_customers_is_active");
        }
    }
}
