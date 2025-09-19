using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class PriceTierConfiguration : IEntityTypeConfiguration<PriceTier>
    {
        public void Configure(EntityTypeBuilder<PriceTier> builder)
        {
            builder.ToTable("PriceTiers");

            builder.HasKey(pt => pt.Id);

            builder.Property(pt => pt.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(pt => pt.Description)
                .HasMaxLength(500);

            builder.OwnsOne(pt => pt.Price, price =>
            {
                price.Property(p => p.Amount)
                    .HasPrecision(18, 2)
                    .IsRequired()
                    .HasColumnName("PriceAmount");

                price.Property(p => p.Currency)
                    .HasMaxLength(3)
                    .IsRequired()
                    .HasColumnName("PriceCurrency");
            });


            builder.Property(pt => pt.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
        }
    }
}
