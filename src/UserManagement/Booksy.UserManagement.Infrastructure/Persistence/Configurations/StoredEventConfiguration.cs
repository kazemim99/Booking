// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/EventSourcing/EventSourcedUserRepository.cs
// ========================================
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Booksy.Infrastructure.Core.Persistence.EventStore;

namespace Booksy.UserManagement.Infrastructure.Persistence.Configurations
{
    public class StoredEventConfiguration : IEntityTypeConfiguration<StoredEvent>
    {
        public void Configure(EntityTypeBuilder<StoredEvent> builder)
        {
            builder.ToTable("event_store", "user_management");

            builder.HasKey(e => e.EventId);

            builder.Property(e => e.EventId)
                .HasColumnName("id");

            builder.Property(e => e.AggregateId)
                .HasColumnName("aggregate_id")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.AggregateType)
                .HasColumnName("aggregate_type")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.EventData)
                .HasColumnName("event_data")
                .HasColumnType("jsonb")
                .IsRequired();

            builder.Property(e => e.Version)
                .HasColumnName("event_version")
                .IsRequired();

            builder.Property(e => e.Timestamp)
                .HasColumnName("timestamp")
                .IsRequired();

      

       

            // Indexes
            builder.HasIndex(e => new { e.AggregateId, e.Version })
                .IsUnique()
                .HasDatabaseName("ix_event_store_aggregate_version");

            builder.HasIndex(e => e.AggregateType)
                .HasDatabaseName("ix_event_store_aggregate_type");

            builder.HasIndex(e => e.EventType)
                .HasDatabaseName("ix_event_store_event_type");

            builder.HasIndex(e => e.Timestamp)
                .HasDatabaseName("ix_event_store_timestamp");


        }
    }
}


