using Booksy.UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.UserManagement.Infrastructure.Persistence.Configurations;

public class PhoneVerificationConfiguration : IEntityTypeConfiguration<PhoneVerification>
{
    public void Configure(EntityTypeBuilder<PhoneVerification> builder)
    {
        builder.ToTable("PhoneVerifications", "user_management");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20)
            .HasComment("Phone number in E.164 format");

        builder.Property(v => v.CountryCode)
            .IsRequired()
            .HasMaxLength(5)
            .HasComment("ISO country code (e.g., DE, US, GB)");

        builder.Property(v => v.HashedCode)
            .IsRequired()
            .HasMaxLength(256)
            .HasComment("Hashed OTP code (SHA256)");

        builder.Property(v => v.ExpiresAt)
            .IsRequired()
            .HasComment("Expiration timestamp for the verification code");

        builder.Property(v => v.IsVerified)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether the phone number has been verified");

        builder.Property(v => v.VerifiedAt)
            .HasComment("Timestamp when verification was completed");

        builder.Property(v => v.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Number of verification attempts made");

        builder.Property(v => v.MaxAttempts)
            .IsRequired()
            .HasDefaultValue(3)
            .HasComment("Maximum allowed verification attempts");

        builder.Property(v => v.IpAddress)
            .HasMaxLength(50)
            .HasComment("IP address of the requester");

        builder.Property(v => v.UserAgent)
            .HasMaxLength(500)
            .HasComment("User agent of the requester");

        builder.Property(v => v.CreatedAt)
            .IsRequired()
          .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'")
            .HasComment("Creation timestamp");

        // Indexes
        builder.HasIndex(v => v.PhoneNumber)
            .HasDatabaseName("IX_PhoneVerifications_PhoneNumber");

        builder.HasIndex(v => v.ExpiresAt)
            .HasDatabaseName("IX_PhoneVerifications_ExpiresAt");

        builder.HasIndex(v => new { v.PhoneNumber, v.IsVerified, v.ExpiresAt })
            .HasDatabaseName("IX_PhoneVerifications_PhoneNumber_Status");
    }
}
