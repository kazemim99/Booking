
//// ========================================
//// Booksy.UserManagement.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs
//// ========================================
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using Booksy.UserManagement.Domain.Entities;
//using Booksy.UserManagement.Domain.ValueObjects;

//namespace Booksy.UserManagement.Infrastructure.Persistence.Configurations
//{
//    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
//    {
//        public void Configure(EntityTypeBuilder<RefreshToken> builder)
//        {
//            builder.ToTable("refresh_tokens", "user_management");

//            builder.HasKey(rt => rt.Id);

//            builder.Property(rt => rt.Id)
//                .HasColumnName("id");

//            builder.Property(rt => rt.Token)
//                .HasColumnName("token")
//                .HasMaxLength(256)
//                .IsRequired();

//            builder.Property(rt => rt.UserId)
//                .HasConversion(
//                    id => id.Value,
//                    value => UserId.From(value))
//                .HasColumnName("user_id")
//                .IsRequired();

//            builder.Property(rt => rt.CreatedAt)
//                .HasColumnName("created_at")
//                .IsRequired();

//            builder.Property(rt => rt.ExpiresAt)
//                .HasColumnName("expires_at")
//                .IsRequired();

//            builder.Property(rt => rt.RevokedAt)
//                .HasColumnName("revoked_at");

//            builder.Property(rt => rt.RevokedReason)
//                .HasColumnName("revoked_reason")
//                .HasMaxLength(500);

//            builder.Property(rt => rt.ReplacedByToken)
//                .HasColumnName("replaced_by_token")
//                .HasMaxLength(256);

//            builder.Property(rt => rt.CreatedByIp)
//                .HasColumnName("created_by_ip")
//                .HasMaxLength(50);

//            builder.Property(rt => rt.RevokedByIp)
//                .HasColumnName("revoked_by_ip")
//                .HasMaxLength(50);

//            // Indexes
//            builder.HasIndex(rt => rt.Token)
//                .IsUnique()
//                .HasDatabaseName("ix_refresh_tokens_token");

//            builder.HasIndex(rt => rt.UserId)
//                .HasDatabaseName("ix_refresh_tokens_user_id");

//            builder.HasIndex(rt => rt.ExpiresAt)
//                .HasDatabaseName("ix_refresh_tokens_expires_at");

//            builder.HasIndex(rt => rt.RevokedAt)
//                .HasDatabaseName("ix_refresh_tokens_revoked_at");
//        }
//    }
//}