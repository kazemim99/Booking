using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
    {
        public void Configure(EntityTypeBuilder<Provider> builder)
        {
            builder.ToTable("Providers", "ServiceCatalog");



            // Primary Key
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Version)
                .IsConcurrencyToken()
                .HasColumnName("Version")
                .HasDefaultValue(0);

            builder.Property(p => p.Id)
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .IsRequired();

            // Owner ID
            builder.Property(p => p.OwnerId)
                .HasConversion(
                    id => id.Value,
                    value => UserId.From(value))
                .IsRequired()
                .HasColumnName("OwnerId");

            // ✅ BUSINESS PROFILE (Owned Entity)
            builder.OwnsOne(p => p.Profile, profile =>
            {
                // Primary properties
                profile.Property(bp => bp.BusinessName)
                    .HasColumnName("BusinessName")
                    .IsRequired()
                    .HasMaxLength(200);

                profile.Property(bp => bp.BusinessDescription)
                    .HasColumnName("BusinessDescription")
                    .IsRequired()
                    .HasMaxLength(2000);

                profile.Property(bp => bp.Website)
                    .HasColumnName("BusinessWebsite")
                    .HasMaxLength(500);

                profile.Property(bp => bp.LogoUrl)
                    .HasColumnName("BusinessLogoUrl")
                    .HasMaxLength(500);

                profile.Property(bp => bp.ProfileImageUrl)
                    .HasColumnName("ProfileImageUrl")
                    .HasMaxLength(500);

                // ✅ SocialMedia as JSON
                profile.Property(bp => bp.SocialMedia)
                    .HasColumnName("BusinessSocialMedia")
                    .HasColumnType("jsonb")
                    .HasConversion(
                        dict => JsonSerializer.Serialize(dict, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
                                ?? new Dictionary<string, string>(),
                        new ValueComparer<Dictionary<string, string>>(
                            (c1, c2) => c1!.SequenceEqual(c2!),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)
                        ))
                    .IsRequired(false);

                // ✅ Tags as JSON
                profile.Property(bp => bp.Tags)
                    .HasColumnName("BusinessTags")
                    .HasColumnType("jsonb")
                    .HasConversion(
                        list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
                                ?? new List<string>(),
                        new ValueComparer<List<string>>(
                            (c1, c2) => c1!.SequenceEqual(c2!),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()
                        ))
                    .IsRequired(false);

                profile.Property(bp => bp.LastUpdatedAt)
                    .HasColumnName("BusinessProfileLastUpdatedAt")
                    .IsRequired()
                    .HasColumnType("timestamp with time zone");

                // Ignore the Id property of BusinessProfile since it's an owned entity
                profile.Ignore(bp => bp.Id);

                // ✅ GALLERY IMAGES (Owned Collection) - FIXED CONFIGURATION
                profile.OwnsMany(bp => bp.GalleryImages, galleryImage =>
                {
                    galleryImage.ToTable("provider_gallery_images");

                    // IMPORTANT: Explicitly configure the foreign key
                    galleryImage.WithOwner()
                        .HasForeignKey("ProviderId");

                    // Configure primary key
                    galleryImage.HasKey(gi => gi.Id);

                    galleryImage.Property(gi => gi.Id)
                        .HasColumnName("id")
                        .ValueGeneratedNever() // Important: Since we generate it in code
                        .IsRequired();

                    galleryImage.Property(gi => gi.ProviderId)
                        .HasConversion(
                            id => id.Value,
                            value => ProviderId.From(value))
                        .HasColumnName("provider_id")
                        .IsRequired();

                    galleryImage.Property(gi => gi.ImageUrl)
                        .HasColumnName("image_url")
                        .IsRequired()
                        .HasMaxLength(500);

                    galleryImage.Property(gi => gi.ThumbnailUrl)
                        .HasColumnName("thumbnail_url")
                        .IsRequired()
                        .HasMaxLength(500);

                    galleryImage.Property(gi => gi.MediumUrl)
                        .HasColumnName("medium_url")
                        .IsRequired()
                        .HasMaxLength(500);

                    galleryImage.Property(gi => gi.DisplayOrder)
                        .HasColumnName("display_order")
                        .IsRequired();

                    galleryImage.Property(gi => gi.Caption)
                        .HasColumnName("caption")
                        .HasMaxLength(500);

                    galleryImage.Property(gi => gi.AltText)
                        .HasColumnName("alt_text")
                        .HasMaxLength(500);

                    galleryImage.Property(gi => gi.UploadedAt)
                        .HasColumnName("uploaded_at")
                        .IsRequired()
                        .HasColumnType("timestamp with time zone");

                    galleryImage.Property(gi => gi.IsActive)
                        .HasColumnName("is_active")
                        .IsRequired()
                        .HasDefaultValue(true);

                    galleryImage.Property(gi => gi.IsPrimary)
                        .HasColumnName("is_primary")
                        .IsRequired()
                        .HasDefaultValue(false);

                    // Note: GalleryImage is an owned entity - concurrency is handled at Provider level
                    // No separate RowVersion needed here to avoid concurrency conflicts on INSERT operations

                    // Indexes
                    galleryImage.HasIndex(gi => new { gi.ProviderId, gi.DisplayOrder })
                        .HasDatabaseName("IX_ProviderGalleryImages_Provider_DisplayOrder");

                    galleryImage.HasIndex(gi => gi.ProviderId)
                        .HasDatabaseName("IX_ProviderGalleryImages_ProviderId");

                    galleryImage.HasIndex(gi => new { gi.ProviderId, gi.IsPrimary })
                        .HasDatabaseName("IX_ProviderGalleryImages_Provider_IsPrimary");
                });

                profile.Navigation(bp => bp.GalleryImages)
                    .UsePropertyAccessMode(PropertyAccessMode.Field)
                    .HasField("_galleryImages"); 
            });



            // Contact Info (Value Object)
            builder.OwnsOne(p => p.ContactInfo, contact =>
            {
                contact.OwnsOne(c => c.Email, email =>
                {
                    email.Property(e => e.Value)
                        .HasMaxLength(320)
                        .IsRequired()
                        .HasColumnName("Email");
                });

                contact.OwnsOne(c => c.PrimaryPhone, phone =>
                {
                    phone.Property(ph => ph.Value)
                        .HasMaxLength(20)
                        .HasColumnName("PrimaryPhoneNumber");

                    phone.Property(ph => ph.CountryCode)
                        .HasMaxLength(5)
                        .HasColumnName("PrimaryPhoneCountryCode");

                    phone.Property(ph => ph.NationalNumber)
                        .HasMaxLength(15)
                        .HasColumnName("PrimaryPhoneNationalNumber");
                });

                contact.OwnsOne(c => c.SecondaryPhone, phone =>
                {
                    phone.Property(ph => ph.Value)
                        .HasMaxLength(20)
                        .HasColumnName("SecondaryPhoneNumber");

                    phone.Property(ph => ph.CountryCode)
                        .HasMaxLength(5)
                        .HasColumnName("SecondaryPhoneCountryCode");

                    phone.Property(ph => ph.NationalNumber)
                        .HasMaxLength(15)
                        .HasColumnName("SecondaryPhoneNationalNumber");
                });

                contact.Property(c => c.Website)
                    .HasMaxLength(500)
                    .HasColumnName("Website");
            });

            // Business Address (Value Object)
            builder.OwnsOne(p => p.Address, address =>
            {
                address.Property(a => a.FormattedAddress)
                    .HasMaxLength(500)
                    .HasColumnName("AddressFormattedAddress");

                address.Property(a => a.Street)
                    .HasMaxLength(200)
                    .IsRequired()
                    .HasColumnName("AddressStreet");

                address.Property(a => a.City)
                    .HasMaxLength(100)
                    .IsRequired()
                    .HasColumnName("AddressCity");

                address.Property(a => a.State)
                    .HasMaxLength(100)
                    .HasColumnName("AddressState");

                address.Property(a => a.PostalCode)
                    .HasMaxLength(20)
                    .IsRequired()
                    .HasColumnName("AddressPostalCode");

                address.Property(a => a.Country)
                    .HasMaxLength(100)
                    .IsRequired()
                    .HasColumnName("AddressCountry");

                address.Property(a => a.ProvinceId)
                    .HasColumnName("AddressProvinceId");

                address.Property(a => a.CityId)
                    .HasColumnName("AddressCityId");

                address.Property(a => a.Latitude)
                    .HasPrecision(10, 8)
                    .HasColumnName("AddressLatitude");

                address.Property(a => a.Longitude)
                    .HasPrecision(11, 8)
                    .HasColumnName("AddressLongitude");
            });

            // Enums
            builder.Property(p => p.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.ProviderType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            // Hierarchy Properties
            builder.Property<Domain.Enums.ProviderHierarchyType>(p => p.HierarchyType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("HierarchyType")
                .HasDefaultValue(Domain.Enums.ProviderHierarchyType.Organization);

            builder.Property(p => p.ParentProviderId)
                .HasConversion(
                    id => id != null ? id.Value : (Guid?)null,
                    value => value.HasValue ? ProviderId.From(value.Value) : null)
                .HasColumnName("ParentProviderId");

            builder.Property(p => p.IsIndependent)
                .IsRequired()
                .HasDefaultValue(false)
                .HasColumnName("IsIndependent");

            // Registration Progress
            builder.Property(p => p.RegistrationStep)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(p => p.IsRegistrationComplete)
                .IsRequired()
                .HasDefaultValue(false);

            // Boolean Properties
            builder.Property(p => p.RequiresApproval)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(p => p.AllowOnlineBooking)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(p => p.OffersMobileServices)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(p => p.PriceRange)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(Domain.Enums.PriceRange.Moderate);

            // Timestamps
            builder.Property(p => p.RegisteredAt)
                .IsRequired();

            builder.Property(p => p.ActivatedAt);
            builder.Property(p => p.VerifiedAt);
            builder.Property(p => p.LastActiveAt);

            // Audit Properties
            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.CreatedBy)
                .HasMaxLength(100);

            builder.Property(p => p.LastModifiedAt);

            builder.Property(p => p.LastModifiedBy)
                .HasMaxLength(100);

            // Navigation Properties
            builder.HasMany(p => p.Staff)
                .WithOne()
                .HasForeignKey("ProviderId")
                .OnDelete(DeleteBehavior.Cascade);

            // Services navigation - using backing field
            builder.HasMany(p => p.Services)
                .WithOne(s => s.Provider)
                .HasForeignKey(s => s.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(p => p.Services)
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasField("_services");

            // BusinessHours as child entities with proper lifecycle management
            builder.HasMany(p => p.BusinessHours)
        .WithOne()
        .HasForeignKey(bh => bh.ProviderId)  // Correct
        .OnDelete(DeleteBehavior.Cascade);


            // Holidays as child entities with proper lifecycle management
            builder.HasMany(p => p.Holidays)
                .WithOne()
                .HasForeignKey("_providerId")
                .OnDelete(DeleteBehavior.Cascade);

            // Exceptions as child entities with proper lifecycle management
            builder.HasMany(p => p.Exceptions)
                .WithOne()
                .HasForeignKey("_providerId")
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(p => p.OwnerId)
                .IsUnique()
                .HasDatabaseName("IX_Providers_OwnerId");

            builder.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Providers_Status");

            builder.HasIndex(p => p.ProviderType)
                .HasDatabaseName("IX_Providers_Type");

            builder.HasIndex(p => (object)p.HierarchyType)
                .HasDatabaseName("IX_Providers_HierarchyType");

            builder.HasIndex(p => p.ParentProviderId)
                .HasDatabaseName("IX_Providers_ParentProviderId");

            builder.HasIndex(p => new { p.HierarchyType, p.IsIndependent })
                .HasDatabaseName("IX_Providers_Hierarchy_Independent");
        }
    }
}
