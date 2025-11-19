// ========================================
// Booksy.UserManagement.Domain/ReadModels/CustomerBookingHistoryEntry.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Domain.ReadModels
{
    /// <summary>
    /// Read model for customer booking history (event-sourced from booking events)
    /// </summary>
    public sealed class CustomerBookingHistoryEntry
    {
        public Guid BookingId { get; private set; }
        public Guid CustomerId { get; private set; }
        public Guid ProviderId { get; private set; }
        public string ProviderName { get; private set; }
        public string ServiceName { get; private set; }
        public DateTime StartTime { get; private set; }
        public string Status { get; private set; }
        public decimal? TotalPrice { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private CustomerBookingHistoryEntry() { }

        public static CustomerBookingHistoryEntry Create(
            Guid bookingId,
            Guid customerId,
            Guid providerId,
            string providerName,
            string serviceName,
            DateTime startTime,
            string status,
            decimal? totalPrice)
        {
            if (bookingId == Guid.Empty)
                throw new ArgumentException("BookingId cannot be empty", nameof(bookingId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("CustomerId cannot be empty", nameof(customerId));

            if (providerId == Guid.Empty)
                throw new ArgumentException("ProviderId cannot be empty", nameof(providerId));

            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("ProviderName is required", nameof(providerName));

            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("ServiceName is required", nameof(serviceName));

            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status is required", nameof(status));

            return new CustomerBookingHistoryEntry
            {
                BookingId = bookingId,
                CustomerId = customerId,
                ProviderId = providerId,
                ProviderName = providerName,
                ServiceName = serviceName,
                StartTime = startTime,
                Status = status,
                TotalPrice = totalPrice,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void UpdateStatus(string newStatus)
        {
            if (string.IsNullOrWhiteSpace(newStatus))
                throw new ArgumentException("Status is required", nameof(newStatus));

            Status = newStatus;
        }

        public bool IsUpcoming()
        {
            return StartTime > DateTime.UtcNow &&
                   (Status == "Confirmed" || Status == "Pending");
        }

        public bool IsPast()
        {
            return StartTime <= DateTime.UtcNow ||
                   Status == "Completed" ||
                   Status == "Cancelled";
        }
    }
}
