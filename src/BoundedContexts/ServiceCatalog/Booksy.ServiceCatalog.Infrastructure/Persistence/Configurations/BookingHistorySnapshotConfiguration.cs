// ========================================
// Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/BookingHistorySnapshotConfiguration.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.Entities;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for BookingHistorySnapshot entity.
/// </summary>
public sealed class BookingHistorySnapshotConfiguration : IEntityTypeConfiguration<BookingHistorySnapshot>
{
    public void Configure(EntityTypeBuilder<BookingHistorySnapshot> builder)
    {
        builder.ToTable("BookingHistorySnapshots", "ServiceCatalog");

        // Primary Key
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .IsRequired()
            .HasColumnName("Id");

        // Booking ID (Value Object - stored as Guid)
        builder.Property(s => s.BookingId)
            .HasConversion(
                id => id.Value,
                value => BookingId.From(value))
            .IsRequired()
            .HasColumnName("BookingId");

        // State ID
        builder.Property(s => s.StateId)
            .IsRequired()
            .HasColumnName("StateId");

        // State Name
        builder.Property(s => s.StateName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("StateName");

        // State JSON (stored as TEXT/JSON)
        builder.Property(s => s.StateJson)
            .IsRequired()
            .HasColumnType("text")
            .HasColumnName("StateJson");

        // Created At
        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasColumnName("CreatedAt");

        // Triggered By
        builder.Property(s => s.TriggeredBy)
            .HasMaxLength(100)
            .HasColumnName("TriggeredBy");

        // Description
        builder.Property(s => s.Description)
            .HasMaxLength(500)
            .HasColumnName("Description");

        // Indexes for performance
        builder.HasIndex(s => s.BookingId)
            .HasDatabaseName("IX_BookingHistorySnapshots_BookingId");

        builder.HasIndex(s => new { s.BookingId, s.StateId })
            .IsUnique()
            .HasDatabaseName("IX_BookingHistorySnapshots_BookingId_StateId");

        builder.HasIndex(s => s.CreatedAt)
            .HasDatabaseName("IX_BookingHistorySnapshots_CreatedAt");
    }
}
