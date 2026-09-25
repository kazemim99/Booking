using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// A salon's customer book. The phone is stored as its normalized E.164 text, one column, so
    /// "one customer per phone per salon" is a plain unique index and every spelling of a number
    /// ("0912 313 5143", "+98912...") lands on the same row.
    /// </summary>
    public sealed class ProviderCustomerConfiguration : IEntityTypeConfiguration<ProviderCustomer>
    {
        public void Configure(EntityTypeBuilder<ProviderCustomer> builder)
        {
            builder.ToTable("provider_customers", "ServiceCatalog");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

            builder.Property(c => c.ProviderId)
                .HasConversion(id => id.Value, value => ProviderId.From(value))
                .HasColumnName("provider_id")
                .IsRequired();

            builder.Property(c => c.FirstName).HasColumnName("first_name").HasMaxLength(ProviderCustomer.MaxNameLength).IsRequired();
            builder.Property(c => c.LastName).HasColumnName("last_name").HasMaxLength(ProviderCustomer.MaxNameLength).IsRequired();

            builder.Property(c => c.PhoneNumber)
                .HasConversion(phone => phone.Value, value => PhoneNumber.From(value))
                .HasColumnName("phone_number")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(c => c.Notes).HasColumnName("notes").HasMaxLength(ProviderCustomer.MaxNotesLength);
            builder.Property(c => c.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

            builder.Ignore(c => c.FullName);

            builder.HasIndex(c => new { c.ProviderId, c.PhoneNumber })
                .IsUnique()
                .HasDatabaseName("ux_provider_customers_provider_phone");
        }
    }
}
