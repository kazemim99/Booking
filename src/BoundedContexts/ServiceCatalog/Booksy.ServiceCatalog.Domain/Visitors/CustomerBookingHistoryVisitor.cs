// ========================================
// Booksy.ServiceCatalog.Domain/Visitors/CustomerBookingHistoryVisitor.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Visitors
{
    /// <summary>
    /// Visitor for analyzing customer booking history and calculating customer lifetime value
    /// </summary>
    public sealed class CustomerBookingHistoryVisitor : IBookingVisitor<CustomerHistoryResult>
    {
        private readonly UserId _customerId;

        private int _totalBookings;
        private int _completedBookings;
        private int _cancelledBookings;
        private decimal _totalSpent;
        private readonly Dictionary<ProviderId, int> _providerBookingCounts = new();
        private readonly List<BookingId> _bookingIds = new();
        private string _currency = "USD";

        public CustomerBookingHistoryVisitor(UserId customerId)
        {
            _customerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
        }

        public void Visit(Booking booking)
        {
            // Only process bookings for the specified customer
            if (booking.CustomerId != _customerId)
                return;

            _totalBookings++;
            _bookingIds.Add(booking.Id);

            // Track status
            if (booking.Status == BookingStatus.Completed)
            {
                _completedBookings++;
                _totalSpent += booking.TotalPrice.Amount;
            }
            else if (booking.Status == BookingStatus.Cancelled)
            {
                _cancelledBookings++;
            }

            // Track provider frequency
            if (_providerBookingCounts.ContainsKey(booking.ProviderId))
            {
                _providerBookingCounts[booking.ProviderId]++;
            }
            else
            {
                _providerBookingCounts[booking.ProviderId] = 1;
            }

            _currency = booking.TotalPrice.Currency;
        }

        public void Visit(Payment payment)
        {
            // Only process payments for the specified customer
            if (payment.CustomerId != _customerId)
                return;

            // Update currency
            if (payment.Amount.Currency != null)
            {
                _currency = payment.Amount.Currency;
            }
        }

        public CustomerHistoryResult GetResult()
        {
            // Find favorite providers (most frequently booked)
            var favoriteProviders = _providerBookingCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .Select(kvp => new ProviderFrequency(kvp.Key, kvp.Value))
                .ToList();

            return new CustomerHistoryResult(
                CustomerId: _customerId,
                TotalBookings: _totalBookings,
                CompletedBookings: _completedBookings,
                CancelledBookings: _cancelledBookings,
                TotalSpent: Money.Create(_totalSpent, _currency),
                FavoriteProviders: favoriteProviders,
                BookingIds: _bookingIds.AsReadOnly()
            );
        }
    }

    /// <summary>
    /// Result of customer history visitor
    /// </summary>
    public sealed record CustomerHistoryResult(
        UserId CustomerId,
        int TotalBookings,
        int CompletedBookings,
        int CancelledBookings,
        Money TotalSpent,
        IReadOnlyList<ProviderFrequency> FavoriteProviders,
        IReadOnlyList<BookingId> BookingIds
    );

    /// <summary>
    /// Provider booking frequency
    /// </summary>
    public sealed record ProviderFrequency(
        ProviderId ProviderId,
        int BookingCount
    );
}
