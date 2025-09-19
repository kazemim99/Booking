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

            builder.Property(ur => ur.Id)
                .HasColumnName("id");

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

