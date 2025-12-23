//using Booksy.Core.Domain.ValueObjects;
//using Booksy.ServiceCatalog.Domain.Aggregates;
//using Booksy.ServiceCatalog.Domain.Entities;
//using Booksy.ServiceCatalog.Domain.ValueObjects;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using System.Numerics;

//namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
//{
//    public sealed class StaffConfiguration : IEntityTypeConfiguration<Staff>
//    {
//        public void Configure(EntityTypeBuilder<Staff> builder)
//        {
//            builder.ToTable("Staff", "ServiceCatalog");

//            // Configure primary key
//            builder.HasKey(s => s.Id);

//            builder.Property(x => x.Id).ValueGeneratedNever();

//            // Configure basic properties
//            builder.Property(s => s.FirstName)
//                .IsRequired()
//                .HasMaxLength(100);

//            builder.Property(s => s.LastName)
//                .IsRequired()
//                .HasMaxLength(100);

//            builder.Property(s => s.Email)
//       .HasConversion(email => email.Value, value => Email.Create(value)).IsRequired(false);

//            builder.Property(s => s.Phone).HasConversion(phone => phone.Value, value => PhoneNumber.From(value));



//            // Configure StaffRole enum
//            builder.Property(s => s.Role)
//                .IsRequired()
//                .HasConversion<string>() // Store as string for readability
//                .HasMaxLength(50);

//            // Configure status and dates
//            builder.Property(s => s.IsActive)
//                .IsRequired()
//                .HasDefaultValue(true);

//            builder.Property(s => s.HiredAt)
//                .IsRequired()
//                .HasColumnType("timestamp with time zone");

//            builder.Property(s => s.TerminatedAt)
//                .HasColumnType("timestamp with time zone");

//            builder.Property(s => s.TerminationReason)
//                .HasMaxLength(500);

//            builder.Property(s => s.Notes)
//                .HasMaxLength(2000);

//            builder.Property(s => s.ProfilePhotoUrl)
//                .HasMaxLength(500);

//            builder.Property(s => s.Biography)
//                .HasMaxLength(500);

//            // Ignore computed properties
//            builder.Ignore(s => s.FullName);

//            // Configure indexes for performance
//            builder.HasIndex(s => s.Email)
//                .HasDatabaseName("IX_Staff_Email");

//            builder.HasIndex(s => s.IsActive)
//                .HasDatabaseName("IX_Staff_IsActive");

//            builder.HasIndex(s => new { s.FirstName, s.LastName })
//                .HasDatabaseName("IX_Staff_Name");


//            // Configure audit fields (if inheriting from Entity base class)
//            ConfigureAuditFields(builder);

//            // Configure relationships (if Staff belongs to Provider)
//            ConfigureRelationships(builder);
//        }

//        private static void ConfigureAuditFields(EntityTypeBuilder<Staff> builder)
//        {
//            // Configure audit fields from Entity base class
//            builder.Property(e => e.CreatedAt)
//                .HasColumnType("timestamp with time zone")
//                .IsRequired();

//            builder.Property(e => e.CreatedBy)
//                .HasMaxLength(100);

//            builder.Property(e => e.LastModifiedAt)
//                .HasColumnType("timestamp with time zone");

//            builder.Property(e => e.LastModifiedBy)
//                .HasMaxLength(100);

//            // Add index on created date for performance
//            builder.HasIndex(e => e.CreatedAt)
//                .HasDatabaseName("IX_Staff_CreatedAt");
//        }

//        private static void ConfigureRelationships(EntityTypeBuilder<Staff> builder)
//        {
          

//            builder.Property(x => x.ProviderId)
//          .HasConversion(
//              v => v.Value,
//              v => ProviderId.From(v))
//          .IsRequired();

//            builder.HasOne<Provider>()
//                .WithMany(p => p.Staff)
//                .HasForeignKey(x => x.ProviderId)
//                .OnDelete(DeleteBehavior.Cascade);
//        }


//    }
//}

