// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/EventSourcing/EventSourcedUserRepository.cs
// ========================================
using Booksy.UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Infrastructure.Persistence.Configurations
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            builder.ToTable("user_profiles", "user_management");

            builder.HasKey(up => up.Id);

            builder.Property(up => up.Id)
                .HasColumnName("id");

            builder.Property(up => up.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(up => up.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(up => up.MiddleName)
                .HasColumnName("middle_name")
                .HasMaxLength(100);

            builder.Property(up => up.DateOfBirth)
                .HasColumnName("date_of_birth");

            builder.Property(up => up.Gender)
                .HasColumnName("gender")
                .HasMaxLength(50);

            builder.OwnsOne(up => up.PhoneNumber, phone =>
            {
                phone.Property(p => p.Value)
                    .HasColumnName("phone_number")
                    .HasMaxLength(50);

                // Map the constructor parameters as properties
                phone.Property(p => p.CountryCode)
                    .HasColumnName("phone_country_code")
                    .HasMaxLength(10);

                phone.Property(p => p.NationalNumber)
                    .HasColumnName("phone_national_number")
                    .HasMaxLength(40);

                // Optional: Add computed column for full international format
                phone.Ignore(p => p.Value); // If Value is computed from CountryCode + NationalNumber
            });

            builder.OwnsOne(up => up.AlternatePhoneNumber, phone =>
            {
                phone.Property(p => p.Value)
                    .HasColumnName("alternate_phone_number")
                    .HasMaxLength(50);

                phone.Property(p => p.CountryCode)
                    .HasColumnName("alternate_phone_country_code")
                    .HasMaxLength(10);

                phone.Property(p => p.NationalNumber)
                    .HasColumnName("alternate_phone_national_number")
                    .HasMaxLength(40);

                phone.Ignore(p => p.Value);
            });


            builder.OwnsOne(up => up.Address, address =>
            {
                address.Property(a => a.Street)
                    .HasColumnName("address_street")
                    .HasMaxLength(200);

                address.Property(a => a.Unit)
                    .HasColumnName("address_unit")
                    .HasMaxLength(50);

                address.Property(a => a.City)
                    .HasColumnName("address_city")
                    .HasMaxLength(100);

                address.Property(a => a.State)
                    .HasColumnName("address_state")
                    .HasMaxLength(100);

                address.Property(a => a.PostalCode)
                    .HasColumnName("address_postal_code")
                    .HasMaxLength(20);

                address.Property(a => a.Country)
                    .HasColumnName("address_country")
                    .HasMaxLength(100);
            });

            builder.Property(up => up.AvatarUrl)
                .HasColumnName("avatar_url")
                .HasMaxLength(500);

            builder.Property(up => up.Bio)
                .HasColumnName("bio")
                .HasMaxLength(1000);

            builder.Property(up => up.PreferredLanguage)
                .HasColumnName("preferred_language")
                .HasMaxLength(10);

            builder.Property(up => up.TimeZone)
                .HasColumnName("time_zone")
                .HasMaxLength(100);

            builder.Property(up => up.Preferences)
                .HasColumnName("preferences")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
                        ?? new Dictionary<string, string>())
                .HasColumnType("jsonb");

            builder.Property(up => up.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(up => up.UpdatedAt)
                .HasColumnName("updated_at");

            // Foreign key to User - configure the existing UserId property
            builder.Property(up => up.UserId)
                .HasConversion(
                    id => id.Value,
                    value => UserId.From(value))
                .HasColumnName("user_id")
                .IsRequired();

            // Indexes
            builder.HasIndex(up => new { up.FirstName, up.LastName })
                .HasDatabaseName("ix_user_profiles_name");
        }
    }
}

