using Booksy.ServiceCatalog.Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class IdempotencyReservationConfiguration : IEntityTypeConfiguration<IdempotencyReservation>
    {
        public void Configure(EntityTypeBuilder<IdempotencyReservation> builder)
        {
            builder.ToTable("IdempotencyReservations", "ServiceCatalog");

            // Composite primary key = the atomic serialization point. Exactly one concurrent INSERT of a given
            // (RequestType, Key) succeeds; the rest get a unique-violation and observe the in-flight/completed row.
            builder.HasKey(r => new { r.RequestType, r.Key });

            builder.Property(r => r.RequestType).HasMaxLength(200).IsRequired();
            builder.Property(r => r.Key).HasMaxLength(200).IsRequired();
            builder.Property(r => r.Status).HasMaxLength(20).IsRequired();
            builder.Property(r => r.ResultJson).HasColumnType("jsonb");
            builder.Property(r => r.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(r => r.CompletedAt).HasColumnType("timestamp with time zone");

            builder.HasIndex(r => r.CreatedAt).HasDatabaseName("IX_IdempotencyReservations_CreatedAt");
        }
    }
}
