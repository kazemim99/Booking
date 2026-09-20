using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations
{
    public sealed class NotificationOutboxEntryConfiguration : IEntityTypeConfiguration<NotificationOutboxEntry>
    {
        public void Configure(EntityTypeBuilder<NotificationOutboxEntry> builder)
        {
            builder.ToTable("NotificationOutbox", "ServiceCatalog");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();

            // Stored by name, not by number. An outbox row is a serialized intent that may sit in the table
            // across a deployment, and a name survives someone reordering the enum; an ordinal does not.
            builder.Property(e => e.EventCode)
                .HasConversion<string>()
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(e => e.RecipientId).IsRequired();
            builder.Property(e => e.SubjectType).HasMaxLength(64);

            builder.Property(e => e.ParametersJson)
                .HasColumnType("jsonb")
                .IsRequired();

            builder.Property(e => e.ScheduledFor).HasColumnType("timestamp with time zone");
            builder.Property(e => e.ClaimedUntil).HasColumnType("timestamp with time zone");
            builder.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(e => e.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.Property(e => e.State).HasMaxLength(20).IsRequired();
            builder.Property(e => e.LastError).HasMaxLength(2000);

            // The de-duplication gate, enforced by the database rather than by a read-then-write. Two
            // concurrent raises of the same event for the same recipient race to INSERT and exactly one wins;
            // the loser gets a unique violation and stands down. A read-check could not promise that.
            builder.HasIndex(e => new { e.DedupKey, e.EventCode, e.RecipientId })
                .IsUnique()
                .HasDatabaseName("UX_NotificationOutbox_Dedup");

            // The sweep's query: pending rows whose time has come, oldest first. Partial, because Processed
            // rows accumulate forever and must not weigh on the hot path.
            builder.HasIndex(e => new { e.State, e.ScheduledFor })
                .HasDatabaseName("IX_NotificationOutbox_Due")
                .HasFilter("\"State\" IN ('Pending', 'Claimed')");

            // Cancelling a booking withdraws its pending reminders, which is a lookup by subject.
            builder.HasIndex(e => new { e.SubjectType, e.SubjectId })
                .HasDatabaseName("IX_NotificationOutbox_Subject");
        }
    }
}
