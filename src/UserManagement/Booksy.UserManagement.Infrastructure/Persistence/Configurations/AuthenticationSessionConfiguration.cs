//// ========================================
//// Booksy.UserManagement.Infrastructure/Persistence/EventSourcing/EventSourcedUserRepository.cs
//// ========================================
//using Booksy.UserManagement.Domain.Entities;
//using Booksy.UserManagement.Domain.ValueObjects;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using Microsoft.EntityFrameworkCore;
//using System.Text.Json;

//namespace Booksy.UserManagement.Infrastructure.Persistence.Configurations
//{
//    public class AuthenticationSessionConfiguration : IEntityTypeConfiguration<AuthenticationSession>
//    {
//        public void Configure(EntityTypeBuilder<AuthenticationSession> builder)
//        {
//            builder.ToTable("authentication_sessions", "user_management");

//            builder.HasKey(s => s.Id);

//            builder.Property(s => s.Id)
//                .HasColumnName("id");

//            builder.Property(s => s.UserId)
//                .HasConversion(
//                    id => id.Value,
//                    value => UserId.From(value))
//                .HasColumnName("user_id")
//                .IsRequired();

//            builder.Property(s => s.SessionToken)
//                .HasColumnName("session_token")
//                .HasMaxLength(256)
//                .IsRequired();

//            builder.Property(s => s.StartedAt)
//                .HasColumnName("started_at")
//                .IsRequired();

//            builder.Property(s => s.EndedAt)
//                .HasColumnName("ended_at");

//            builder.Property(s => s.LastActivityAt)
//                .HasColumnName("last_activity_at")
//                .IsRequired();

//            builder.Property(s => s.ExpiresAt)
//                .HasColumnName("expires_at")
//                .IsRequired();

//            builder.Property(s => s.IpAddress)
//                .HasColumnName("ip_address")
//                .HasMaxLength(50);

//            builder.Property(s => s.UserAgent)
//                .HasColumnName("user_agent")
//                .HasMaxLength(500);

//            builder.Property(s => s.DeviceId)
//                .HasColumnName("device_id")
//                .HasMaxLength(100);

//            builder.Property(s => s.DeviceName)
//                .HasColumnName("device_name")
//                .HasMaxLength(200);

//            builder.Property(s => s.Metadata)
//                .HasColumnName("metadata")
//                .HasConversion(
//                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
//                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)
//                        ?? new Dictionary<string, object>())
//                .HasColumnType("jsonb");

//            // Indexes
//            builder.HasIndex(s => s.SessionToken)
//                .IsUnique()
//                .HasDatabaseName("ix_authentication_sessions_token");

//            builder.HasIndex(s => s.UserId)
//                .HasDatabaseName("ix_authentication_sessions_user_id");

//            builder.HasIndex(s => s.ExpiresAt)
//                .HasDatabaseName("ix_authentication_sessions_expires_at");

//            builder.HasIndex(s => s.EndedAt)
//                .HasDatabaseName("ix_authentication_sessions_ended_at");
//        }
//    }
//}