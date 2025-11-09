using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Entity configuration for GalleryImage
    /// GalleryImage is a child entity of Provider (not an owned entity)
    /// </summary>
    public sealed class GalleryImageConfiguration : IEntityTypeConfiguration<GalleryImage>
    {
        public void Configure(EntityTypeBuilder<GalleryImage> builder)
        {
            builder.ToTable("provider_gallery_images");

            // Primary key
            builder.HasKey(gi => gi.Id);

            builder.Property(gi => gi.Id)
                .HasColumnName("id")
                .ValueGeneratedNever() // Generated in code via GalleryImage.Create()
                .IsRequired();

            // Foreign key to Provider
            builder.Property(gi => gi.ProviderId)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .HasColumnName("provider_id")
                .IsRequired();

            // Image URLs
            builder.Property(gi => gi.ImageUrl)
                .HasColumnName("image_url")
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(gi => gi.ThumbnailUrl)
                .HasColumnName("thumbnail_url")
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(gi => gi.MediumUrl)
                .HasColumnName("medium_url")
                .IsRequired()
                .HasMaxLength(500);

            // Display and ordering
            builder.Property(gi => gi.DisplayOrder)
                .HasColumnName("display_order")
                .IsRequired();

            // Metadata
            builder.Property(gi => gi.Caption)
                .HasColumnName("caption")
                .HasMaxLength(500);

            builder.Property(gi => gi.AltText)
                .HasColumnName("alt_text")
                .HasMaxLength(500);

            // Timestamps
            builder.Property(gi => gi.UploadedAt)
                .HasColumnName("uploaded_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            // State flags
            builder.Property(gi => gi.IsActive)
                .HasColumnName("is_active")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(gi => gi.IsPrimary)
                .HasColumnName("is_primary")
                .IsRequired()
                .HasDefaultValue(false);

            // Concurrency token
            builder.Property<byte[]>("RowVersion")
                .IsConcurrencyToken()
                .HasColumnName("row_version")
                .ValueGeneratedOnAddOrUpdate();

            // Indexes for efficient queries
            builder.HasIndex(gi => new { gi.ProviderId, gi.DisplayOrder })
                .HasDatabaseName("IX_ProviderGalleryImages_Provider_DisplayOrder");

            builder.HasIndex(gi => gi.ProviderId)
                .HasDatabaseName("IX_ProviderGalleryImages_ProviderId");

            builder.HasIndex(gi => new { gi.ProviderId, gi.IsPrimary })
                .HasDatabaseName("IX_ProviderGalleryImages_Provider_IsPrimary");
        }
    }
}
