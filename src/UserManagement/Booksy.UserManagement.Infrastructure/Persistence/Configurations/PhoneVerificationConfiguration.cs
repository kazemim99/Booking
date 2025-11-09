// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Configurations/PhoneVerificationConfiguration.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates.PhoneVerificationAggregate;
using Booksy.UserManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.UserManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PhoneVerification aggregate
/// </summary>
public class PhoneVerificationConfiguration : IEntityTypeConfiguration<PhoneVerification>
{
    public void Configure(EntityTypeBuilder<PhoneVerification> builder)
    {
        builder.ToTable("phone_verifications", "user_management");

        // Primary Key - VerificationId value object
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasConversion(
                id => id.Value,
                value => VerificationId.From(value))
            .HasColumnName("id")
            .IsRequired();

        // UserId - nullable value object
        builder.Property(v => v.UserId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.From(value.Value) : null)
            .HasColumnName("user_id");

        // PhoneNumber - owned value object
        builder.OwnsOne(v => v.PhoneNumber, pn =>
        {
            pn.Property(p => p.Value)
                .HasColumnName("phone_number")
                .HasMaxLength(20)
                .IsRequired()
                .HasComment("Phone number in international format");

            pn.Property(p => p.CountryCode)
                .HasColumnName("country_code")
                .HasMaxLength(5)
                .IsRequired()
                .HasComment("Country code (e.g., +98, +1)");

            pn.Property(p => p.NationalNumber)
                .HasColumnName("national_number")
                .HasMaxLength(15)
                .IsRequired()
                .HasComment("National number without country code");
        });

        // OtpCode - owned value object (for validation, not persisted hash)
        // We'll store the generated OTP temporarily, but in production should only store hash
        builder.Ignore(v => v.OtpCode);

        builder.Property(v => v.OtpHash)
            .HasColumnName("otp_hash")
            .HasMaxLength(256)
            .IsRequired()
            .HasComment("SHA256 hashed OTP code");

        // Enums - stored as strings
        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Current status of verification");

        builder.Property(v => v.Method)
            .HasConversion<string>()
            .HasColumnName("method")
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Verification method (SMS, Call, WhatsApp)");

        builder.Property(v => v.Purpose)
            .HasConversion<string>()
            .HasColumnName("purpose")
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Purpose of verification (Registration, Login, etc.)");

        // Attempt Tracking
        builder.Property(v => v.SendAttempts)
            .HasColumnName("send_attempts")
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Number of times OTP was sent");

        builder.Property(v => v.VerificationAttempts)
            .HasColumnName("verification_attempts")
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Number of verification attempts made");

        builder.Property(v => v.MaxVerificationAttempts)
            .HasColumnName("max_verification_attempts")
            .IsRequired()
            .HasDefaultValue(5)
            .HasComment("Maximum allowed verification attempts");

        builder.Property(v => v.LastSentAt)
            .HasColumnName("last_sent_at")
            .HasComment("Timestamp when OTP was last sent");

        builder.Property(v => v.LastAttemptAt)
            .HasColumnName("last_attempt_at")
            .HasComment("Timestamp of last verification attempt");

        // Lifecycle Timestamps
        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasComment("Creation timestamp");

        builder.Property(v => v.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired()
            .HasComment("Expiration timestamp for the verification code");

        builder.Property(v => v.VerifiedAt)
            .HasColumnName("verified_at")
            .HasComment("Timestamp when verification was completed");

        builder.Property(v => v.BlockedUntil)
            .HasColumnName("blocked_until")
            .HasComment("Timestamp until which verification is blocked");

        // Metadata
        builder.Property(v => v.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(50)
            .HasComment("IP address of the requester");

        builder.Property(v => v.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500)
            .HasComment("User agent of the requester");

        builder.Property(v => v.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(100)
            .HasComment("Session identifier");

        // Ignore domain events (not persisted)
        builder.Ignore(v => v.DomainEvents);

        // Indexes for query performance
        builder.HasIndex(v => v.PhoneNumber.Value)
            .HasDatabaseName("ix_phone_verifications_phone_number");

        builder.HasIndex(v => v.Status)
            .HasDatabaseName("ix_phone_verifications_status");

        builder.HasIndex(v => v.ExpiresAt)
            .HasDatabaseName("ix_phone_verifications_expires_at");

        builder.HasIndex(v => v.CreatedAt)
            .HasDatabaseName("ix_phone_verifications_created_at");

        // Composite index for common queries
        builder.HasIndex(v => new { v.PhoneNumber.Value, v.Purpose, v.Status })
            .HasDatabaseName("ix_phone_verifications_phone_purpose_status");
    }
}
