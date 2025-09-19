using Booksy.ServiceCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class BusinessHoursConfiguration : IEntityTypeConfiguration<BusinessHours>
    {
        public void Configure(EntityTypeBuilder<BusinessHours> builder)
        {
            builder.ToTable("BusinessHours");

            builder.HasKey(bh => bh.Id);

            builder.Property(bh => bh.DayOfWeek)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.OwnsOne(bh => bh.OperatingHours, hours =>
            {
                hours.Property(h => h.StartTime)
                    .IsRequired()
                    .HasColumnName("OpenTime");

                hours.Property(h => h.EndTime)
                    .IsRequired()
                    .HasColumnName("CloseTime");
            });

            builder.Property(bh => bh.IsOpen)
                .IsRequired()
                .HasDefaultValue(true);

     

            // Indexes
            builder.HasIndex(bh => bh.DayOfWeek)
                .HasDatabaseName("IX_BusinessHours_DayOfWeek");
        }
    }
}
