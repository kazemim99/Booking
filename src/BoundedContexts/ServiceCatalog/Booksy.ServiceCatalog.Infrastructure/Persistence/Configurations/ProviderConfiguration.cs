using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
    {
        public void Configure(EntityTypeBuilder<Provider> builder)
        {
            builder.ToTable("Providers");

            // Primary Key
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .HasConversion(
                    id => id.Value,
                    value =>  ProviderId.From(value))
                .IsRequired();

            // Owner ID
            builder.Property(p => p.OwnerId)
                .HasConversion(
                    id => id.Value,
                    value =>  UserId.From(value))
                .IsRequired()
                .HasColumnName("OwnerId");

            // Business Profile (Value Object)
            builder.OwnsOne(p => p.Profile, profile =>
            {
                profile.Property(bp => bp.BusinessName)
                    .HasMaxLength(200)
                    .IsRequired()
                    .HasColumnName("BusinessName");

                profile.Property(bp => bp.Description)
                    .HasMaxLength(1000)
                    .HasColumnName("Description");
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

            builder.Property(p => p.Type)
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

            builder.HasMany(p => p.BusinessHours)
                .WithOne()
                .HasForeignKey("ProviderId")
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(p => p.OwnerId)
                .IsUnique()
                .HasDatabaseName("IX_Providers_OwnerId");

            builder.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Providers_Status");

            builder.HasIndex(p => p.Type)
                .HasDatabaseName("IX_Providers_Type");
        }
    }
}
