// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/Entities/BookingHistoryEntry.cs
// ========================================
using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate
{
    /// <summary>
    /// Represents an entry in the booking history timeline
    /// </summary>
    public sealed class BookingHistoryEntry : Entity<Guid>
    {
        public string Description { get; private set; }
        public BookingStatus Status { get; private set; }
        public DateTime OccurredAt { get; private set; }

        // Private constructor for EF Core
        private BookingHistoryEntry() : base() { }

        private BookingHistoryEntry(string description, BookingStatus status)
        {
            Id = Guid.NewGuid();
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Status = status;
            OccurredAt = DateTime.UtcNow;
        }

        internal static BookingHistoryEntry Create(string description, BookingStatus status)
        {
            return new BookingHistoryEntry(description, status);
        }
    }
}
