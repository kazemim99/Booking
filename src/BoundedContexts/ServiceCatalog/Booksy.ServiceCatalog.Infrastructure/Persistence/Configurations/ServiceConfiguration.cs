using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.ToTable("Services");

            // Primary Key
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id)
                .HasConversion(
                    id => id.Value,
                    value => ServiceId.From(value))
                .IsRequired();

            // Provider ID
            builder.Property(s => s.ProviderId)
                .HasConversion(
                    id => id.Value,
                    value =>  ProviderId.From(value))
                .IsRequired()
                .HasColumnName("ProviderId");

            // Core Information
            builder.Property(s => s.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(s => s.Description)
                .HasMaxLength(1000)
                .IsRequired();

            // Service Category (Value Object)
            builder.OwnsOne(s => s.Category, category =>
            {
                category.Property(c => c.Name)
                    .HasMaxLength(100)
                    .IsRequired()
                    .HasColumnName("CategoryName");

                category.Property(c => c.Description)
                    .HasMaxLength(500)
                    .HasColumnName("CategoryDescription");

                category.Property(c => c.IconUrl)
                    .HasMaxLength(500)
                    .HasColumnName("CategoryIconUrl");
            });

            // Service Type
            builder.Property(s => s.Type)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            // Base Price (Value Object)
            builder.OwnsOne(s => s.BasePrice, price =>
            {
                price.Property(p => p.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired()
                    .HasColumnName("BasePriceAmount");

                price.Property(p => p.Currency)
                    .HasMaxLength(3)
                    .IsRequired()
                    .HasColumnName("BasePriceCurrency");
            });

            // Duration (Value Object)
            builder.Property(s => s.Duration)
                .HasConversion(
                    duration => duration.Value,
                    value => Duration.FromMinutes(value))
                .IsRequired()
                .HasColumnName("DurationMinutes");

            // Preparation Time
            builder.Property(s => s.PreparationTime)
                .HasConversion(
                    duration => duration != null ? duration.Value : (int?)null,
                    value => value.HasValue ? Duration.FromMinutes(value.Value) : null)
                .HasColumnName("PreparationTimeMinutes");

            // Buffer Time
            builder.Property(s => s.BufferTime)
                .HasConversion(
                    duration => duration != null ? duration.Value : (int?)null,
                    value => value.HasValue ? Duration.FromMinutes(value.Value) : null)
                .HasColumnName("BufferTimeMinutes");

            // Status
            builder.Property(s => s.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            // Boolean Properties
            builder.Property(s => s.RequiresDeposit)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(s => s.AllowOnlineBooking)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.AvailableAtLocation)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.AvailableAsMobile)
                .IsRequired()
                .HasDefaultValue(false);

            // Numeric Properties
            builder.Property(s => s.DepositPercentage)
                .HasPrecision(5, 2)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(s => s.MaxAdvanceBookingDays)
                .IsRequired()
                .HasDefaultValue(30);

            builder.Property(s => s.MinAdvanceBookingHours)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(s => s.MaxConcurrentBookings)
                .IsRequired()
                .HasDefaultValue(1);

            // Metadata
            builder.Property(s => s.ImageUrl)
                .HasMaxLength(500);

            // Tags as JSON
            //builder.Property(e => e.Tags)
            //    .HasColumnName("Tags")
            //    .HasColumnType("text[]")
            //    .HasDefaultValue(new string[0]); // Empty array default

            // Metadata as JSON
            //builder.Property(s => s.Metadata)
            //    .HasConversion(
            //        metadata => JsonSerializer.Serialize(metadata, (JsonSerializerOptions?)null),
            //        json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
            //    .HasColumnType("nvarchar(max)")
            //    .HasDefaultValue("{}");

            // Audit Properties
            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.CreatedBy)
                .HasMaxLength(100);

            builder.Property(s => s.LastModifiedAt);

            builder.Property(s => s.LastModifiedBy)
                .HasMaxLength(100);

            // Navigation Properties
            builder.HasMany(s => s.Options)
                .WithOne()
                .HasForeignKey("ServiceId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.PriceTiers)
                .WithOne()
                .HasForeignKey("ServiceId")
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(s => s.ProviderId)
                .HasDatabaseName("IX_Services_ProviderId");

            builder.HasIndex(s => s.Status)
                .HasDatabaseName("IX_Services_Status");

            builder.HasIndex(s => s.Type)
                .HasDatabaseName("IX_Services_Type");

            builder.HasIndex(s => new { s.ProviderId, s.Name })
                .IsUnique()
                .HasDatabaseName("IX_Services_ProviderId_Name");
        }
    }
}
