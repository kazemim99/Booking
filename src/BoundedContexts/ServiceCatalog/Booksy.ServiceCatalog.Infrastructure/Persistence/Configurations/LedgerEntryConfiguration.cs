using Booksy.ServiceCatalog.Domain.Aggregates.LedgerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
    {
        public void Configure(EntityTypeBuilder<LedgerEntry> builder)
        {
            builder.ToTable("LedgerEntries", "ServiceCatalog");

            builder.HasKey(e => e.Id);

            // Client-generated GUID key (Guid.NewGuid() in the entity). ValueGeneratedNever so EF never mis-classifies
            // a newly-posted entry as an existing row — the aggregate-persistence-concurrency lesson.
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.EventId).IsRequired();

            builder.Property(e => e.Account).HasConversion<string>().HasMaxLength(40).IsRequired();
            builder.Property(e => e.Direction).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(e => e.EntryType).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.OwnsOne(e => e.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
            });

            builder.Property(e => e.BookingId);
            builder.Property(e => e.PaymentId);
            builder.Property(e => e.ProviderId);
            builder.Property(e => e.PostedAt).HasColumnType("timestamp with time zone").IsRequired();

            // Idempotency: a given money event posts to each account at most once. Redelivery is a no-op / rejected.
            builder.HasIndex(e => new { e.EventId, e.Account })
                .IsUnique()
                .HasDatabaseName("UX_LedgerEntries_EventId_Account");

            builder.HasIndex(e => e.PaymentId).HasDatabaseName("IX_LedgerEntries_PaymentId");
            builder.HasIndex(e => e.BookingId).HasDatabaseName("IX_LedgerEntries_BookingId");
            builder.HasIndex(e => e.ProviderId).HasDatabaseName("IX_LedgerEntries_ProviderId");
        }
    }
}
