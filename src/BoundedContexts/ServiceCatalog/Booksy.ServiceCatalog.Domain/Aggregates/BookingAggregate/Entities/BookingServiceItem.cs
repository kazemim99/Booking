// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/Entities/BookingServiceItem.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.Entities
{
    /// <summary>
    /// One service line inside a booking. A booking may bundle several
    /// services performed back-to-back in a single visit (e.g. cut + color);
    /// the booking's TimeSlot/TotalPrice are the sums over these lines.
    /// Persisted as a JSON collection on the booking row — line items are
    /// read/written only through their aggregate, never queried relationally.
    /// </summary>
    public sealed class BookingServiceItem
    {
        public Guid ServiceId { get; private set; }
        public string Name { get; private set; }
        public decimal Price { get; private set; }
        public string Currency { get; private set; }
        public int DurationMinutes { get; private set; }

        // Private constructor for EF Core / JSON materialization
        private BookingServiceItem()
        {
            Name = string.Empty;
            Currency = string.Empty;
        }

        public BookingServiceItem(
            Guid serviceId,
            string name,
            decimal price,
            string currency,
            int durationMinutes)
        {
            if (serviceId == Guid.Empty)
                throw new ArgumentException("Service id cannot be empty", nameof(serviceId));
            if (durationMinutes <= 0)
                throw new ArgumentException("Duration must be positive", nameof(durationMinutes));
            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));

            ServiceId = serviceId;
            Name = name ?? string.Empty;
            Price = price;
            Currency = currency ?? string.Empty;
            DurationMinutes = durationMinutes;
        }
    }
}
