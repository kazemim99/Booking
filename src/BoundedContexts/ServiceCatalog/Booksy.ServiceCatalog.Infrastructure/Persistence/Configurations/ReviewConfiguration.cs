// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.ToTable("Reviews", "ServiceCatalog");

            // Primary Key
            builder.HasKey(r => r.Id);

            // Concurrency Token
            builder.Property(r => r.Version)
                .IsConcurrencyToken()
                .HasColumnName("Version")
                .HasDefaultValue(0);

            // ID
            builder.Property(r => r.Id)
                .IsRequired()
                .HasColumnName("ReviewId");

            // Provider ID (Value Object)
            builder.Property(r => r.ProviderId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .IsRequired()
                .HasColumnName("ProviderId");

            // Customer ID (Value Object)
            builder.Property(r => r.CustomerId)
                .HasConversion(
                    id => id.Value,
                    value => UserId.From(value))
                .IsRequired()
                .HasColumnName("CustomerId");

            // Booking ID
            builder.Property(r => r.BookingId)
                .IsRequired()
                .HasColumnName("BookingId");

            // Rating Value
            builder.Property(r => r.RatingValue)
                .IsRequired()
                .HasColumnName("RatingValue")
                .HasColumnType("decimal(3,1)") // e.g., 4.5
                .HasPrecision(3, 1);

            // Comment (Persian/English text)
            builder.Property(r => r.Comment)
                .IsRequired(false)
                .HasColumnName("Comment")
                .HasMaxLength(2000);

            // Verification Status
            builder.Property(r => r.IsVerified)
                .IsRequired()
                .HasColumnName("IsVerified")
                .HasDefaultValue(true);

            // Provider Response
            builder.Property(r => r.ProviderResponse)
                .IsRequired(false)
                .HasColumnName("ProviderResponse")
                .HasMaxLength(1000);

            builder.Property(r => r.ProviderResponseAt)
                .IsRequired(false)
                .HasColumnName("ProviderResponseAt")
                .HasColumnType("timestamp with time zone");

            // Helpfulness Counts
            builder.Property(r => r.HelpfulCount)
                .IsRequired()
                .HasColumnName("HelpfulCount")
                .HasDefaultValue(0);

            builder.Property(r => r.NotHelpfulCount)
                .IsRequired()
                .HasColumnName("NotHelpfulCount")
                .HasDefaultValue(0);

            // Audit Properties
            builder.Property(r => r.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(r => r.CreatedBy)
                .IsRequired(false)
                .HasColumnName("CreatedBy")
                .HasMaxLength(100);

            builder.Property(r => r.LastModifiedAt)
                .IsRequired(false)
                .HasColumnName("LastModifiedAt")
                .HasColumnType("timestamp with time zone");

            builder.Property(r => r.LastModifiedBy)
                .IsRequired(false)
                .HasColumnName("LastModifiedBy")
                .HasMaxLength(100);

            // Indexes for query performance
            builder.HasIndex(r => r.ProviderId)
                .HasDatabaseName("IX_Reviews_ProviderId");

            builder.HasIndex(r => r.CustomerId)
                .HasDatabaseName("IX_Reviews_CustomerId");

            builder.HasIndex(r => r.BookingId)
                .HasDatabaseName("IX_Reviews_BookingId")
                .IsUnique(); // One review per booking

            builder.HasIndex(r => new { r.ProviderId, r.RatingValue })
                .HasDatabaseName("IX_Reviews_Provider_Rating");

            builder.HasIndex(r => new { r.ProviderId, r.CreatedAt })
                .HasDatabaseName("IX_Reviews_Provider_CreatedAt");

            builder.HasIndex(r => new { r.IsVerified, r.CreatedAt })
                .HasDatabaseName("IX_Reviews_Verified_CreatedAt");
        }
    }
}
