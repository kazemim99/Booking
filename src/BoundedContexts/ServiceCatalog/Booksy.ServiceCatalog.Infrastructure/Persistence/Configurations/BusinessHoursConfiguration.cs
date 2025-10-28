using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessHours = Booksy.ServiceCatalog.Domain.Entities.BusinessHours;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class BusinessHoursConfiguration : IEntityTypeConfiguration<BusinessHours>
    {
        public void Configure(EntityTypeBuilder<BusinessHours> builder)
        {
            builder.ToTable("BusinessHours", "ServiceCatalog");

            // Primary Key
            builder.HasKey(bh => bh.Id);

            builder.Property(bh => bh.Id)
                .IsRequired()
                .ValueGeneratedNever(); 


            builder.Property(bh => bh.ProviderId)
                .HasColumnName("ProviderId")
                .HasConversion(
                    id => id.Value,
                    value => ProviderId.From(value))
                .IsRequired();



            builder.HasOne<Provider>()
    .WithMany(p => p.BusinessHours)
    .HasForeignKey(bh => bh.ProviderId)
    .OnDelete(DeleteBehavior.Cascade)
    .IsRequired();


            // Day of Week
            builder.Property(bh => bh.DayOfWeek)
                .IsRequired()
                .HasConversion<int>();

            // Operating Times
            builder.Property(bh => bh.OpenTime)
                .HasColumnType("time");

            builder.Property(bh => bh.CloseTime)
                .HasColumnType("time");

            // Audit Properties
            builder.Property(bh => bh.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(bh => bh.CreatedBy)
                .HasMaxLength(100);

            builder.Property(bh => bh.LastModifiedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(bh => bh.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(bh => bh.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            // Configure Breaks as owned collection within BusinessHours
            builder.OwnsMany(bh => bh.Breaks, breaks =>
            {
                breaks.ToTable("BreakPeriods", "ServiceCatalog");

                breaks.WithOwner().HasForeignKey("BusinessHoursId");
                breaks.Property<int>("Id")
                    .ValueGeneratedOnAdd();
                breaks.HasKey("Id");

                breaks.Property(b => b.StartTime)
                    .IsRequired()
                    .HasColumnType("time");

                breaks.Property(b => b.EndTime)
                    .IsRequired()
                    .HasColumnType("time");

                breaks.Property(b => b.Label)
                    .HasMaxLength(100);
            });

            // Indexes
            builder.HasIndex(bh => bh.DayOfWeek);

            builder.HasIndex(bh => bh.ProviderId)
    .HasDatabaseName("IX_BusinessHours_ProviderId");


            // Query Filter for soft deletes
            builder.HasQueryFilter(bh => !bh.IsDeleted);
        }
    }
}
