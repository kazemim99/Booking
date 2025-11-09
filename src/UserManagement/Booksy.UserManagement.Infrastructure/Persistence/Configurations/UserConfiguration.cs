// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Configurations/UserConfiguration.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using System.Text.Json;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users", "user_management");

            // Primary Key
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .HasConversion(
                    id => id.Value,
                    value => UserId.From(value))
                .HasColumnName("id");

            // ===========================
            // CONCURRENCY CONFIGURATION FOR POSTGRESQL
            // ===========================



            builder.OwnsOne(u => u.PhoneNumber, pn =>
            {
                pn.Property(p => p.Value)
                    .HasColumnName("PhoneNumber")
                    .HasMaxLength(20);

                pn.Property(p => p.CountryCode)
                    .HasColumnName("CountryCode")
                    .HasMaxLength(5);

                pn.Property(p => p.NationalNumber)
                    .HasColumnName("NationalNumber")
                    .HasMaxLength(15);
            });

            // Option 1: Using integer Version (RECOMMENDED - Most explicit and portable)
            builder.Property(u => u.Version)
                .HasColumnName("version")
                .IsConcurrencyToken()
                .HasDefaultValue(0)
                .IsRequired();

            builder.OwnsOne(u => u.Email, email =>
            {
                email.Property(e => e.Value)
                    .HasColumnName("email")
                    .HasMaxLength(256)
                    .IsRequired();

                email.HasIndex(e => e.Value)
                    .IsUnique()
                    .HasDatabaseName("ix_users_email");
            });


            builder.OwnsOne(u => u.Password, password =>
            {
                password.Property(p => p.Hash)
                    .HasColumnName("password_hash")
                    .HasMaxLength(500)
                    .IsRequired();
            });

            // Profile relationship
            builder.HasOne(u => u.Profile)
                .WithOne()
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Status and Type
            builder.Property(u => u.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(u => u.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            // Timestamps
            builder.Property(u => u.RegisteredAt)
                .HasColumnName("registered_at")
                .IsRequired();

            builder.Property(u => u.ActivatedAt)
                .HasColumnName("activated_at");

            builder.Property(u => u.LastLoginAt)
                .HasColumnName("last_login_at");

            builder.Property(u => u.LastPasswordChangeAt)
                .HasColumnName("last_password_change_at");

            builder.Property(u => u.DeactivatedAt)
                .HasColumnName("deactivated_at");

            // Security Settings
            builder.Property(u => u.TwoFactorEnabled)
                .HasColumnName("two_factor_enabled")
                .HasDefaultValue(false);

            builder.Property(u => u.FailedLoginAttempts)
                .HasColumnName("failed_login_attempts")
                .HasDefaultValue(0);

            builder.Property(u => u.LockedUntil)
                .HasColumnName("locked_until");

            // Tokens
            builder.OwnsOne(u => u.ActivationToken, token =>
            {
                token.Property(t => t.Token)
                    .HasColumnName("activation_token")
                    .HasMaxLength(256);

                token.Property(t => t.CreatedAt)
                    .HasColumnName("activation_token_created_at");

                token.Property(t => t.ExpiresAt)
                    .HasColumnName("activation_token_expires_at");
            });

            builder.OwnsOne(u => u.PasswordResetToken, token =>
            {
                token.Property(t => t.Token)
                    .HasColumnName("password_reset_token")
                    .HasMaxLength(256);

                token.Property(t => t.CreatedAt)
                    .HasColumnName("password_reset_token_created_at");

                token.Property(t => t.ExpiresAt)
                    .HasColumnName("password_reset_token_expires_at");
            });


            builder.OwnsMany(u => u.RefreshTokens, rt =>
            {
                rt.ToTable("refresh_tokens", "user_management");
                rt.WithOwner().HasForeignKey("UserId");
                rt.Property<Guid>("Id").ValueGeneratedOnAdd();
                rt.HasKey("Id");

                rt.Property(r => r.Token).HasColumnName("token").HasMaxLength(256).IsRequired();
                rt.Property(r => r.CreatedAt).HasColumnName("created_at");
                rt.Property(r => r.ExpiresAt).HasColumnName("expires_at");
                rt.Property(r => r.RevokedAt).HasColumnName("revoked_at");
                rt.Property(r => r.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(500);
                rt.Property(r => r.ReplacedByToken).HasColumnName("replaced_by_token").HasMaxLength(256);
                rt.Property(r => r.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(50);
                rt.Property(r => r.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(50);



                rt.HasIndex(r => r.Token).IsUnique().HasDatabaseName("ix_refresh_tokens_token");
            });

            // ✅ OWNED: ActiveSessions
            builder.OwnsMany(u => u.ActiveSessions, s =>
            {
                s.ToTable("authentication_sessions", "user_management");
                s.WithOwner().HasForeignKey("UserId");
                s.Property<Guid>("Id").ValueGeneratedOnAdd();
                s.HasKey("Id");

                s.Property(a => a.SessionToken).HasColumnName("session_token").HasMaxLength(256).IsRequired();
                s.Property(a => a.DeviceId).HasColumnName("device_info").HasMaxLength(500);
                s.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
                s.Property(a => a.StartedAt).HasColumnName("started_at");
                s.Property(a => a.LastActivityAt).HasColumnName("last_activity_at");
                s.Property(a => a.ExpiresAt).HasColumnName("expires_at");
                s.Property(a => a.EndedAt).HasColumnName("ended_at");
                s.Property(la => la.Metadata)
               .HasColumnName("metadata")
               .HasConversion(
                   v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                   v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)
                       ?? new Dictionary<string, object>())
               .HasColumnType("jsonb");

                s.HasIndex(a => a.SessionToken).IsUnique().HasDatabaseName("ix_sessions_token");
            });

            // Collections
            builder.HasMany(u => u.Roles)
                .WithOne()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.OwnsMany(u => u.RecentLoginAttempts, la =>
            {
                la.ToTable("login_attempts", "user_management");

                // EF Core will create shadow properties for Id and UserId
                la.WithOwner().HasForeignKey("UserId");

                // Configure shadow primary key
                la.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("id");

                la.HasKey("Id");

                // Configure properties
                la.Property(l => l.AttemptedAt)
                    .HasColumnName("attempted_at")
                    .IsRequired();

                la.Property(l => l.IpAddress)
                    .HasColumnName("ip_address")
                    .HasMaxLength(50);

                la.Property(l => l.UserAgent)
                    .HasColumnName("user_agent")
                    .HasMaxLength(500);

                la.Property(l => l.IsSuccessful)
                    .HasColumnName("was_successful");

                la.Property(l => l.FailureReason)
                    .HasColumnName("failure_reason")
                    .HasMaxLength(500);

                la.Property(la => la.Metadata)
                    .HasColumnName("metadata")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)
                            ?? new Dictionary<string, object>())
                    .HasColumnType("jsonb");

                // Index for querying
                la.HasIndex("UserId", "AttemptedAt")
                    .HasDatabaseName("ix_login_attempts_user_attempted");
            });

            // Ignore domain events (not persisted)
            builder.Ignore(u => u.DomainEvents);

            // Indexes
            builder.HasIndex(u => u.Status)
                .HasDatabaseName("ix_users_status");

            builder.HasIndex(u => u.Type)
                .HasDatabaseName("ix_users_type");

            builder.HasIndex(u => u.RegisteredAt)
                .HasDatabaseName("ix_users_registered_at");
        }
    }
}