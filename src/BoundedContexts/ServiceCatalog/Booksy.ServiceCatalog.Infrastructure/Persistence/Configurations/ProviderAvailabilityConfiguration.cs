// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/ProviderAvailabilityConfiguration.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ProviderAvailabilityConfiguration : IEntityTypeConfiguration<ProviderAvailability>
    {
        public void Configure(EntityTypeBuilder<ProviderAvailability> builder)
        {
            builder.ToTable("ProviderAvailability", "ServiceCatalog");

            // Primary Key
            builder.HasKey(a => a.Id);

            // Concurrency Token
            builder.Property(a => a.Version)
                .IsConcurrencyToken()
                .HasColumnName("Version")
                .HasDefaultValue(0);

            // ID
            builder.Property(a => a.Id)
                .IsRequired()
                .HasColumnName("AvailabilityId");

            // Provider ID (Value Object)
            builder.Property(a => a.ProviderId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .IsRequired()
                .HasColumnName("ProviderId");

            // Staff ID (Optional)
            builder.Property(a => a.StaffId)
                .IsRequired(false)
                .HasColumnName("StaffId");

            // Date
            builder.Property(a => a.Date)
                .IsRequired()
                .HasColumnName("Date")
                .HasColumnType("date");

            // Time Slot
            builder.Property(a => a.StartTime)
                .IsRequired()
                .HasColumnName("StartTime")
                .HasColumnType("time");

            builder.Property(a => a.EndTime)
                .IsRequired()
                .HasColumnName("EndTime")
                .HasColumnType("time");

            // Status (Enum as string)
            builder.Property(a => a.Status)
                .IsRequired()
                .HasColumnName("Status")
                .HasConversion<string>()
                .HasMaxLength(50);

            // Booking ID (Optional)
            builder.Property(a => a.BookingId)
                .IsRequired(false)
                .HasColumnName("BookingId");

            // Block Reason (Optional)
            builder.Property(a => a.BlockReason)
                .IsRequired(false)
                .HasColumnName("BlockReason")
                .HasMaxLength(500);

            // Hold Expiration (Optional)
            builder.Property(a => a.HoldExpiresAt)
                .IsRequired(false)
                .HasColumnName("HoldExpiresAt")
                .HasColumnType("timestamp with time zone");

            // Audit Properties
            builder.Property(a => a.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(a => a.CreatedBy)
                .IsRequired(false)
                .HasColumnName("CreatedBy")
                .HasMaxLength(100);

            builder.Property(a => a.LastModifiedAt)
                .IsRequired(false)
                .HasColumnName("LastModifiedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(a => a.LastModifiedBy)
                .IsRequired(false)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(100);

            // Indexes for query performance
            builder.HasIndex(a => new { a.ProviderId, a.Date, a.StartTime })
                .HasDatabaseName("IX_ProviderAvailability_Provider_Date_StartTime");

            builder.HasIndex(a => new { a.Date, a.Status })
                .HasDatabaseName("IX_ProviderAvailability_Date_Status");

            builder.HasIndex(a => a.BookingId)
                .HasDatabaseName("IX_ProviderAvailability_BookingId")
                .IsUnique(false);

            builder.HasIndex(a => new { a.HoldExpiresAt, a.Status })
                .HasDatabaseName("IX_ProviderAvailability_HoldExpiration_Status")
                .HasFilter("\"HoldExpiresAt\" IS NOT NULL");

            // Ignore navigation properties (not mapped in initial version)
            // builder.Ignore(a => a.DomainEvents); // Already handled by base class
        }
    }
}
