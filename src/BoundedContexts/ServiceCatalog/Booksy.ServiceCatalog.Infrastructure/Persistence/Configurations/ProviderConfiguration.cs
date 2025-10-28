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
            builder.ToTable("Providers");



            // Primary Key
            builder.HasKey(p => p.Id);

            // Ignore Version property (not using optimistic concurrency for now)
            builder.Ignore(p => p.Version);

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

                // ✅ SocialMedia as JSON (Dictionary<string, string>)
                profile.Property(bp => bp.SocialMedia)
                    .HasColumnName("BusinessSocialMedia")
                    .HasColumnType("jsonb")  // Use 'nvarchar(max)' for SQL Server
                    .HasConversion(
                        dict => JsonSerializer.Serialize(dict, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
                                ?? new Dictionary<string, string>(),
                        new ValueComparer<Dictionary<string, string>>(
                            (c1, c2) => c1!.SequenceEqual(c2!),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToDictionary(x => x.Key, x => x.Value)
                        ))
                    .IsRequired(false);

                // ✅ Tags as JSON (List<string>)
                profile.Property(bp => bp.Tags)
                    .HasColumnName("BusinessTags")
                    .HasColumnType("jsonb")  // Use 'nvarchar(max)' for SQL Server
                    .HasConversion(
                        tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
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
                    phone.Property(ph => ph.CountryCode)
                        .HasMaxLength(5)
                        .HasColumnName("PrimaryPhoneCountryCode");

                    phone.Property(ph => ph.Value)
                        .HasMaxLength(20)
                        .HasColumnName("PrimaryPhoneNumber");
                });

                contact.OwnsOne(c => c.SecondaryPhone, phone =>
                {
                    phone.Property(ph => ph.CountryCode)
                        .HasMaxLength(5)
                        .HasColumnName("SecondaryPhoneCountryCode");

                    phone.Property(ph => ph.Value)
                        .HasMaxLength(20)
                        .HasColumnName("SecondaryPhoneNumber");
                });

                contact.Property(c => c.Website)
                    .HasMaxLength(500)
                    .HasColumnName("Website");
            });

            // Business Address (Value Object)
            builder.OwnsOne(p => p.Address, address =>
            {
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
        }
    }
}
