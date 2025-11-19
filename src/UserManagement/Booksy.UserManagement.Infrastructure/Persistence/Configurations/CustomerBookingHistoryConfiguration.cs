// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Configurations/CustomerBookingHistoryConfiguration.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Booksy.UserManagement.Domain.ReadModels;

namespace Booksy.UserManagement.Infrastructure.Persistence.Configurations
{
    public class CustomerBookingHistoryConfiguration : IEntityTypeConfiguration<CustomerBookingHistoryEntry>
    {
        public void Configure(EntityTypeBuilder<CustomerBookingHistoryEntry> builder)
        {
            builder.ToTable("customer_booking_history", "user_management");

            // Primary Key
            builder.HasKey(b => b.BookingId);

            builder.Property(b => b.BookingId)
                .HasColumnName("booking_id")
                .IsRequired();

            builder.Property(b => b.CustomerId)
                .HasColumnName("customer_id")
                .IsRequired();

            builder.Property(b => b.ProviderId)
                .HasColumnName("provider_id")
                .IsRequired();

            builder.Property(b => b.ProviderName)
                .HasColumnName("provider_name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(b => b.ServiceName)
                .HasColumnName("service_name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(b => b.StartTime)
                .HasColumnName("start_time")
                .IsRequired();

            builder.Property(b => b.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(b => b.TotalPrice)
                .HasColumnName("total_price")
                .HasColumnType("decimal(10,2)");

            builder.Property(b => b.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            // Indexes for efficient queries
            builder.HasIndex(b => new { b.CustomerId, b.StartTime })
                .HasDatabaseName("ix_customer_booking_history_customer_time")
                .IsDescending(false, true); // DESC on StartTime for latest first

            builder.HasIndex(b => b.Status)
                .HasDatabaseName("ix_customer_booking_history_status");

            builder.HasIndex(b => b.ProviderId)
                .HasDatabaseName("ix_customer_booking_history_provider");
        }
    }
}
