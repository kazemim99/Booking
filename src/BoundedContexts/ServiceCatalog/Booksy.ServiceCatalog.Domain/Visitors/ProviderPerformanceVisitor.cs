// ========================================
// Booksy.ServiceCatalog.Domain/Visitors/ProviderPerformanceVisitor.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Visitors
{
    /// <summary>
    /// Visitor for calculating provider performance metrics
    /// </summary>
    public sealed class ProviderPerformanceVisitor : IBookingVisitor<ProviderPerformanceResult>
    {
        private readonly ProviderId _providerId;
        private readonly DateTime? _startDate;
        private readonly DateTime? _endDate;

        private int _totalBookings;
        private int _completedBookings;
        private int _cancelledBookings;
        private int _noShowBookings;
        private decimal _totalRevenue;
        private decimal _totalBookingDurationMinutes;
        private readonly Dictionary<ServiceId, int> _serviceBookingCounts = new();
        private string _currency = "USD";

        public ProviderPerformanceVisitor(ProviderId providerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            _providerId = providerId ?? throw new ArgumentNullException(nameof(providerId));
            _startDate = startDate;
            _endDate = endDate;
        }

        public void Visit(Booking booking)
        {
            // Only process bookings for the specified provider
            if (booking.ProviderId != _providerId)
                return;

            // Apply date filters
            if (_startDate.HasValue && booking.RequestedAt < _startDate.Value)
                return;

            if (_endDate.HasValue && booking.RequestedAt > _endDate.Value)
                return;

            _totalBookings++;

            // Track status
            switch (booking.Status)
            {
                case BookingStatus.Completed:
                    _completedBookings++;
                    _totalRevenue += booking.TotalPrice.Amount;
                    _totalBookingDurationMinutes += booking.Duration.Minutes;
                    break;
                case BookingStatus.Cancelled:
                    _cancelledBookings++;
                    break;
                case BookingStatus.NoShow:
                    _noShowBookings++;
                    break;
            }

            // Track service popularity
            if (_serviceBookingCounts.ContainsKey(booking.ServiceId))
            {
                _serviceBookingCounts[booking.ServiceId]++;
            }
            else
            {
                _serviceBookingCounts[booking.ServiceId] = 1;
            }

            _currency = booking.TotalPrice.Currency;
        }

        public void Visit(Payment payment)
        {
            // Only process payments for the specified provider
            if (payment.ProviderId != _providerId)
                return;

            // Apply date filters
            if (_startDate.HasValue && payment.CreatedAt < _startDate.Value)
                return;

            if (_endDate.HasValue && payment.CreatedAt > _endDate.Value)
                return;

            // Update currency
            if (payment.Amount.Currency != null)
            {
                _currency = payment.Amount.Currency;
            }
        }

        public ProviderPerformanceResult GetResult()
        {
            var averageBookingDuration = _completedBookings > 0
                ? _totalBookingDurationMinutes / _completedBookings
                : 0;

            var completionRate = _totalBookings > 0
                ? (decimal)_completedBookings / _totalBookings
                : 0;

            // Find top services by booking count
            var topServices = _serviceBookingCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .Select(kvp => new ServicePopularity(kvp.Key, kvp.Value))
                .ToList();

            return new ProviderPerformanceResult(
                ProviderId: _providerId,
                TotalBookings: _totalBookings,
                CompletedBookings: _completedBookings,
                CancelledBookings: _cancelledBookings,
                NoShowBookings: _noShowBookings,
                TotalRevenue: Money.Create(_totalRevenue, _currency),
                AverageBookingDurationMinutes: averageBookingDuration,
                CompletionRate: completionRate,
                TopServices: topServices
            );
        }
    }

    /// <summary>
    /// Result of provider performance visitor
    /// </summary>
    public sealed record ProviderPerformanceResult(
        ProviderId ProviderId,
        int TotalBookings,
        int CompletedBookings,
        int CancelledBookings,
        int NoShowBookings,
        Money TotalRevenue,
        decimal AverageBookingDurationMinutes,
        decimal CompletionRate,
        IReadOnlyList<ServicePopularity> TopServices
    );

    /// <summary>
    /// Service popularity by booking count
    /// </summary>
    public sealed record ServicePopularity(
        ServiceId ServiceId,
        int BookingCount
    );
}
