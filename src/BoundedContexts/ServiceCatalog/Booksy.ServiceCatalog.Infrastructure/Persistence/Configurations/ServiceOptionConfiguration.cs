// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/ServiceOptionConfiguration.cs
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ServiceOptionConfiguration : IEntityTypeConfiguration<ServiceOption>
    {
        public void Configure(EntityTypeBuilder<ServiceOption> builder)
        {
            builder.ToTable("ServiceOptions", "servicecatalog");

            // Primary Key
            builder.HasKey(so => so.Id);

            // Properties
            builder.Property(so => so.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(so => so.Description)
                .HasMaxLength(500);

            // Additional Price (Owned Value Object)
            builder.OwnsOne(so => so.AdditionalPrice, price =>
            {
                price.Property(p => p.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired()
                    .HasColumnName("AdditionalPriceAmount");

                price.Property(p => p.Currency)
                    .HasMaxLength(3)
                    .IsRequired()
                    .HasColumnName("AdditionalPriceCurrency");
            });

            // Additional Duration
            builder.Property(so => so.AdditionalDuration)
                .HasConversion(
                    duration => duration != null ? duration.Value : (int?)null,
                    value => value.HasValue ? Duration.FromMinutes(value.Value) : null)
                .HasColumnName("AdditionalDurationMinutes");

            // Boolean flags
            builder.Property(so => so.IsRequired)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(so => so.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Sort order
            builder.Property(so => so.SortOrder)
                .IsRequired()
                .HasDefaultValue(0);

            // Timestamp
            builder.Property(so => so.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes
            builder.HasIndex(so => so.IsActive)
                .HasDatabaseName("IX_ServiceOptions_IsActive");

            builder.HasIndex(so => so.SortOrder)
                .HasDatabaseName("IX_ServiceOptions_SortOrder");

            builder.HasIndex(so => new { so.IsActive, so.SortOrder })
                .HasDatabaseName("IX_ServiceOptions_Active_SortOrder");
        }
    }
}