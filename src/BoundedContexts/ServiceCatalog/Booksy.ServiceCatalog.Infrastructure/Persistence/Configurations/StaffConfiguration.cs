using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Numerics;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class StaffConfiguration : IEntityTypeConfiguration<Staff>
    {
        public void Configure(EntityTypeBuilder<Staff> builder)
        {
            builder.ToTable("Staff", "servicecatalog");

            // Configure primary key
            builder.HasKey(s => s.Id);

            // Configure basic properties
            builder.Property(s => s.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Email)
       .HasConversion(email => email.Value, value => Email.Create(value));

            builder.Property(s => s.Phone)
.HasConversion(phone => phone.Value, value => PhoneNumber.Create(value));



            // Configure StaffRole enum
            builder.Property(s => s.Role)
                .IsRequired()
                .HasConversion<string>() // Store as string for readability
                .HasMaxLength(50);

            // Configure status and dates
            builder.Property(s => s.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.HiredAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(s => s.TerminatedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(s => s.TerminationReason)
                .HasMaxLength(500);

            builder.Property(s => s.Notes)
                .HasMaxLength(2000);

            // Ignore computed properties
            builder.Ignore(s => s.FullName);

            // Configure indexes for performance
            builder.HasIndex(s => s.Email)
                .HasDatabaseName("IX_Staff_Email");

            builder.HasIndex(s => s.IsActive)
                .HasDatabaseName("IX_Staff_IsActive");

            builder.HasIndex(s => new { s.FirstName, s.LastName })
                .HasDatabaseName("IX_Staff_Name");


            // Configure audit fields (if inheriting from Entity base class)
            ConfigureAuditFields(builder);

            // Configure relationships (if Staff belongs to Provider)
            ConfigureRelationships(builder);
        }

        private static void ConfigureAuditFields(EntityTypeBuilder<Staff> builder)
        {
            // Configure audit fields if they exist in the base Entity class
            builder.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property<DateTime>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Add index on created date for performance
            builder.HasIndex("CreatedAt")
                .HasDatabaseName("IX_Staff_CreatedAt");
        }

        private static void ConfigureRelationships(EntityTypeBuilder<Staff> builder)
        {
            // If Staff belongs to Provider, configure the relationship
            // This assumes there's a ProviderId foreign key property
            // You may need to adjust this based on your actual domain model

            // Option 1: If Staff has a ProviderId property
            // builder.Property<Guid>("ProviderId")
            //     .IsRequired();

            //builder.HasOne<Provider>()
            //    .WithMany(p => p.Staff)
            //    .HasForeignKey("ProviderId")
            //    .OnDelete(DeleteBehavior.Cascade);

            // Option 2: If you need to add ProviderId as shadow property
            builder.Property<ProviderId>("ProviderId")
                .IsRequired();

            builder.HasIndex("ProviderId")
                .HasDatabaseName("IX_Staff_ProviderId");
        }


    }
}

