using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class ExceptionScheduleConfiguration : IEntityTypeConfiguration<ExceptionSchedule>
    {
        public void Configure(EntityTypeBuilder<ExceptionSchedule> builder)
        {
            builder.ToTable("ProviderExceptions", "ServiceCatalog");

            // Primary Key
            builder.HasKey(e => e.Id);

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
    .WithMany(p => p.Exceptions)
    .HasForeignKey(bh => bh.ProviderId)
    .OnDelete(DeleteBehavior.Cascade)
    .IsRequired();

            // Properties
            builder.Property(e => e.Date)
                .IsRequired()
                .HasColumnType("date");

            builder.Property(e => e.OpenTime)
                .HasColumnType("time");

            builder.Property(e => e.CloseTime)
                .HasColumnType("time");

            builder.Property(e => e.Reason)
                .IsRequired()
                .HasMaxLength(500);

            // Audit Properties
            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            builder.Property(e => e.LastModifiedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(e => e.Date)
                .HasDatabaseName("IX_ProviderExceptions_Date");


            // Query Filter for soft deletes
            builder.HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
