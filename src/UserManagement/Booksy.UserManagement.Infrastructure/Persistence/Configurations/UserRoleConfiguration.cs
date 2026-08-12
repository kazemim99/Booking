// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Context/UserManagementDbContext.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Booksy.UserManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Infrastructure.Persistence.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("user_roles", "user_management");

            builder.HasKey(ur => ur.Id);

            // UserRole.Id is assigned by the domain (Guid.NewGuid() in UserRole.Create),
            // not by the store. Without ValueGeneratedNever, EF's convention for a Guid
            // key is ValueGeneratedOnAdd, and its "is the key already set?" heuristic
            // then classifies a freshly-constructed role as a PRE-EXISTING row: the new
            // entity is tracked Modified instead of Added, so SaveChanges emits
            // UPDATE user_roles ... WHERE id = <never-inserted id>, affects 0 rows, and
            // throws DbUpdateConcurrencyException.
            //
            // That broke every provider OTP sign-in for a phone that already had an
            // account: one-person-per-phone reuses the person and grants the missing
            // capacity (EnsureCanActAs -> AddRole -> a brand-new UserRole on an already
            // tracked aggregate), which is exactly this append.
            //
            // Same class of defect, and same fix, as Transaction.Id / DeliveryAttempt.Id
            // / PriceTier.Id / BookingHistoryEntry.Id / BookingServiceItem elsewhere in
            // the solution. Model-side metadata only — the column is unchanged, so this
            // needs no migration.
            builder.Property(ur => ur.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(ur => ur.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(ur => ur.AssignedAt)
                .HasColumnName("assigned_at")
                .IsRequired();

            builder.Property(ur => ur.ExpiresAt)
                .HasColumnName("expires_at");

            builder.Property(ur => ur.AssignedBy)
                .HasColumnName("assigned_by")
                .HasMaxLength(100);

            // Store metadata as JSON
            builder.Property(ur => ur.Metadata)
                .HasColumnName("metadata")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)
                        ?? new Dictionary<string, object>())
                .HasColumnType("jsonb");

            // Foreign key to User
            builder.Property<UserId>("UserId")
                .HasColumnName("user_id");

            builder.HasIndex("UserId", "Name")
                .IsUnique()
                .HasDatabaseName("ix_user_roles_user_id_name");

            builder.HasIndex(ur => ur.Name)
                .HasDatabaseName("ix_user_roles_name");

            builder.HasIndex(ur => ur.ExpiresAt)
                .HasDatabaseName("ix_user_roles_expires_at");
        }
    }
}

