// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetBookingStatistics/GetBookingStatisticsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingStatistics
{
    public sealed class GetBookingStatisticsQueryHandler : IQueryHandler<GetBookingStatisticsQuery, BookingStatisticsDto>
    {
        private readonly IBookingReadRepository _bookingRepository;
        private readonly ILogger<GetBookingStatisticsQueryHandler> _logger;

        public GetBookingStatisticsQueryHandler(
            IBookingReadRepository bookingRepository,
            ILogger<GetBookingStatisticsQueryHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task<BookingStatisticsDto> Handle(GetBookingStatisticsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting booking statistics for provider {ProviderId}", request.ProviderId);

            // Get statistics from repository (domain model)
            var domainStats = await _bookingRepository.GetStatisticsAsync(
                request.ProviderId,
                request.StartDate,
                request.EndDate,
                cancellationToken);

            _logger.LogInformation("Retrieved statistics for provider {ProviderId}: {TotalBookings} total bookings",
                request.ProviderId, domainStats.TotalBookings);

            // Map domain model to application DTO
            return new BookingStatisticsDto(
                TotalBookings: domainStats.TotalBookings,
                RequestedBookings: domainStats.RequestedBookings,
                ConfirmedBookings: domainStats.ConfirmedBookings,
                CompletedBookings: domainStats.CompletedBookings,
                CancelledBookings: domainStats.CancelledBookings,
                NoShowBookings: domainStats.NoShowBookings,
                RescheduledBookings: domainStats.RescheduledBookings,
                TotalRevenue: domainStats.TotalRevenue,
                CompletedRevenue: domainStats.CompletedRevenue,
                PendingRevenue: domainStats.PendingRevenue,
                RefundedAmount: domainStats.RefundedAmount,
                Currency: domainStats.Currency,
                CompletionRate: domainStats.CompletionRate,
                NoShowRate: domainStats.NoShowRate,
                CancellationRate: domainStats.CancellationRate,
                StartDate: domainStats.StartDate,
                EndDate: domainStats.EndDate);
        }
    }
}
