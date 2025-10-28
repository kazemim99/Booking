using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class HolidayScheduleConfiguration : IEntityTypeConfiguration<HolidaySchedule>
    {
        public void Configure(EntityTypeBuilder<HolidaySchedule> builder)
        {
            builder.ToTable("ProviderHolidays", "ServiceCatalog");

            // Primary Key
            builder.HasKey(h => h.Id);


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
                    .WithMany(p => p.Holidays)
                    .HasForeignKey(bh => bh.ProviderId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

            // Properties
            builder.Property(h => h.Date)
                .IsRequired()
                .HasColumnType("date");

            builder.Property(h => h.Reason)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(h => h.IsRecurring)
                .IsRequired();

            builder.Property(h => h.Pattern)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Audit Properties
            builder.Property(h => h.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(h => h.CreatedBy)
                .HasMaxLength(100);

            builder.Property(h => h.LastModifiedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(h => h.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(h => h.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(h => h.Date)
                .HasDatabaseName("IX_ProviderHolidays_Date");

            // Query Filter for soft deletes
            builder.HasQueryFilter(h => !h.IsDeleted);
        }
    }
}
