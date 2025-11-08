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

            // Use the new statistics method from repository
            var stats = await _bookingRepository.GetStatisticsAsync(
                request.ProviderId,
                request.StartDate,
                request.EndDate,
                cancellationToken);

            _logger.LogInformation("Retrieved statistics for provider {ProviderId}: {TotalBookings} total bookings",
                request.ProviderId, stats.TotalBookings);

            return stats;
        }
    }
}
