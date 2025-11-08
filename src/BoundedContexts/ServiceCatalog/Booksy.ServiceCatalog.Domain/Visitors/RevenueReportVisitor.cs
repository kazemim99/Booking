// ========================================
// Booksy.ServiceCatalog.Domain/Visitors/RevenueReportVisitor.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Visitors
{
    /// <summary>
    /// Visitor for calculating revenue statistics from bookings and payments
    /// </summary>
    public sealed class RevenueReportVisitor : IBookingVisitor<RevenueReportResult>
    {
        private readonly ProviderId? _filterProviderId;
        private readonly DateTime? _startDate;
        private readonly DateTime? _endDate;

        private decimal _totalRevenue;
        private decimal _totalBookingValue;
        private int _bookingCount;
        private int _paidBookingCount;
        private readonly Dictionary<DateTime, decimal> _revenueByDate = new();
        private string _currency = "USD"; // Default currency

        public RevenueReportVisitor(ProviderId? filterProviderId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            _filterProviderId = filterProviderId;
            _startDate = startDate;
            _endDate = endDate;
        }

        public void Visit(Booking booking)
        {
            // Apply filters
            if (_filterProviderId != null && booking.ProviderId != _filterProviderId)
                return;

            if (_startDate.HasValue && booking.RequestedAt < _startDate.Value)
                return;

            if (_endDate.HasValue && booking.RequestedAt > _endDate.Value)
                return;

            // Count all bookings and their total value
            _bookingCount++;
            _totalBookingValue += booking.TotalPrice.Amount;
            _currency = booking.TotalPrice.Currency;

            // Only count completed bookings as revenue
            if (booking.Status == BookingStatus.Completed)
            {
                _paidBookingCount++;
                _totalRevenue += booking.TotalPrice.Amount;

                // Track revenue by date
                var bookingDate = booking.RequestedAt.Date;
                if (_revenueByDate.ContainsKey(bookingDate))
                {
                    _revenueByDate[bookingDate] += booking.TotalPrice.Amount;
                }
                else
                {
                    _revenueByDate[bookingDate] = booking.TotalPrice.Amount;
                }
            }
        }

        public void Visit(Payment payment)
        {
            // Apply filters
            if (_filterProviderId != null && payment.ProviderId != _filterProviderId)
                return;

            if (_startDate.HasValue && payment.CreatedAt < _startDate.Value)
                return;

            if (_endDate.HasValue && payment.CreatedAt > _endDate.Value)
                return;

            // Update currency if not set
            if (payment.Amount.Currency != null)
            {
                _currency = payment.Amount.Currency;
            }
        }

        public RevenueReportResult GetResult()
        {
            var averageBookingValue = _bookingCount > 0 ? _totalBookingValue / _bookingCount : 0;

            var revenueByDate = _revenueByDate
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new DailyRevenue(kvp.Key, kvp.Value, _currency))
                .ToList();

            return new RevenueReportResult(
                Money.Create(_totalRevenue, _currency),
                Money.Create(averageBookingValue, _currency),
                _bookingCount,
                _paidBookingCount,
                revenueByDate
            );
        }
    }

    /// <summary>
    /// Result of revenue report visitor
    /// </summary>
    public sealed record RevenueReportResult(
        Money TotalRevenue,
        Money AverageBookingValue,
        int TotalBookings,
        int PaidBookings,
        IReadOnlyList<DailyRevenue> RevenueByDate
    );

    /// <summary>
    /// Revenue for a specific date
    /// </summary>
    public sealed record DailyRevenue(
        DateTime Date,
        decimal Amount,
        string Currency
    );
}
