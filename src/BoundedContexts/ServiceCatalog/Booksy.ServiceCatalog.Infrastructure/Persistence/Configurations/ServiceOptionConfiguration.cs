using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ServiceOptionConfiguration : IEntityTypeConfiguration<ServiceOption>
    {
        public void Configure(EntityTypeBuilder<ServiceOption> builder)
        {
            builder.ToTable("ServiceOptions");

            builder.HasKey(so => so.Id);

            builder.Property(so => so.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(so => so.Description)
                .HasMaxLength(500);

            builder.OwnsOne(so => so.AdditionalPrice, price =>
            {
                price.Property(p => p.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired()
                    .HasColumnName("AdditionalPriceAmount");

                price.Property(p => p.Currency)
                    .HasMaxLength(3)
                    .IsRequired()
                    .HasColumnName("AdditionalPriceCurrency");
            });

            builder.Property(so => so.AdditionalDuration)
                .HasConversion(
                    duration => duration != null ? duration.Value : (int?)null,
                    value => value.HasValue ? Duration.FromMinutes(value.Value) : null)
                .HasColumnName("AdditionalDurationMinutes");

            builder.Property(so => so.IsRequired)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(so => so.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
        }
    }
}

