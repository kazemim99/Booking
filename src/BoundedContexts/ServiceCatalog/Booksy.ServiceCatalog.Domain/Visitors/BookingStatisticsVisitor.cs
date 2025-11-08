// ========================================
// Booksy.ServiceCatalog.Domain/Visitors/BookingStatisticsVisitor.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Visitors
{
    /// <summary>
    /// Visitor for calculating booking statistics
    /// </summary>
    public sealed class BookingStatisticsVisitor : IBookingVisitor<BookingStatisticsResult>
    {
        private readonly ProviderId? _filterProviderId;
        private readonly DateTime? _startDate;
        private readonly DateTime? _endDate;

        private int _totalBookings;
        private int _requestedBookings;
        private int _confirmedBookings;
        private int _completedBookings;
        private int _cancelledBookings;
        private int _rescheduledBookings;
        private int _noShowBookings;

        public BookingStatisticsVisitor(ProviderId? filterProviderId = null, DateTime? startDate = null, DateTime? endDate = null)
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

            // Count total bookings
            _totalBookings++;

            // Count by status
            switch (booking.Status)
            {
                case BookingStatus.Requested:
                    _requestedBookings++;
                    break;
                case BookingStatus.Confirmed:
                    _confirmedBookings++;
                    break;
                case BookingStatus.Completed:
                    _completedBookings++;
                    break;
                case BookingStatus.Cancelled:
                    _cancelledBookings++;
                    break;
                case BookingStatus.Rescheduled:
                    _rescheduledBookings++;
                    break;
                case BookingStatus.NoShow:
                    _noShowBookings++;
                    break;
            }
        }

        public void Visit(Payment payment)
        {
            // No payment-specific logic needed for booking statistics
        }

        public BookingStatisticsResult GetResult()
        {
            var completionRate = _totalBookings > 0 ? (decimal)_completedBookings / _totalBookings : 0;
            var cancellationRate = _totalBookings > 0 ? (decimal)_cancelledBookings / _totalBookings : 0;
            var noShowRate = _totalBookings > 0 ? (decimal)_noShowBookings / _totalBookings : 0;

            return new BookingStatisticsResult(
                Total: _totalBookings,
                Requested: _requestedBookings,
                Confirmed: _confirmedBookings,
                Completed: _completedBookings,
                Cancelled: _cancelledBookings,
                Rescheduled: _rescheduledBookings,
                NoShows: _noShowBookings,
                CompletionRate: completionRate,
                CancellationRate: cancellationRate,
                NoShowRate: noShowRate
            );
        }
    }

    /// <summary>
    /// Result of booking statistics visitor
    /// </summary>
    public sealed record BookingStatisticsResult(
        int Total,
        int Requested,
        int Confirmed,
        int Completed,
        int Cancelled,
        int Rescheduled,
        int NoShows,
        decimal CompletionRate,
        decimal CancellationRate,
        decimal NoShowRate
    );
}
