//// ========================================
//// Booksy.UserManagement.Infrastructure/Persistence/EventSourcing/EventSourcedUserRepository.cs
//// ========================================
//using Booksy.UserManagement.Domain.Entities;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using Microsoft.EntityFrameworkCore;
//using System.Text.Json;
//using Booksy.UserManagement.Domain.ValueObjects;

//namespace Booksy.UserManagement.Infrastructure.Persistence.Configurations
//{
//    public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
//    {
//        public void Configure(EntityTypeBuilder<LoginAttempt> builder)
//        {
//            builder.ToTable("login_attempts", "user_management");

//            builder.HasKey(la => la.Id);

//            builder.Property(la => la.Id)
//                .HasColumnName("id");

//            builder.Property(la => la.AttemptedAt)
//                .HasColumnName("attempted_at")
//                .IsRequired();

//            builder.Property(la => la.IpAddress)
//                .HasColumnName("ip_address")
//                .HasMaxLength(50);

//            builder.Property(la => la.UserAgent)
//                .HasColumnName("user_agent")
//                .HasMaxLength(500);

//            builder.Property(la => la.IsSuccessful)
//                .HasColumnName("is_successful")
//                .IsRequired();

//            builder.Property(la => la.FailureReason)
//                .HasColumnName("failure_reason")
//                .HasMaxLength(500);

//            builder.Property(la => la.Metadata)
//                .HasColumnName("metadata")
//                .HasConversion(
//                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
//                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)
//                        ?? new Dictionary<string, object>())
//                .HasColumnType("jsonb");


//            builder.Property(la => la.UserId)
//    .HasConversion(
//        id => id.Value,
//        value => UserId.From(value))
//    .HasColumnName("user_id")
//    .IsRequired(); // This MUST be set!


       

//            // Indexes
//            builder.HasIndex(la => la.AttemptedAt)
//                .HasDatabaseName("ix_login_attempts_attempted_at");

//            builder.HasIndex(la => la.IsSuccessful)
//                .HasDatabaseName("ix_login_attempts_is_successful");

//            builder.HasIndex("UserId")
//                .HasDatabaseName("ix_login_attempts_user_id");
//        }
//    }
//}


