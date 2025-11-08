// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/SearchBookings/SearchBookingsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.SearchBookings
{
    public sealed class SearchBookingsQueryHandler : IQueryHandler<SearchBookingsQuery, SearchBookingsResult>
    {
        private readonly IBookingReadRepository _bookingRepository;
        private readonly ILogger<SearchBookingsQueryHandler> _logger;

        public SearchBookingsQueryHandler(
            IBookingReadRepository bookingRepository,
            ILogger<SearchBookingsQueryHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task<SearchBookingsResult> Handle(SearchBookingsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Searching bookings with criteria: ProviderId={ProviderId}, CustomerId={CustomerId}, Status={Status}",
                request.ProviderId, request.CustomerId, request.Status);

            // Use the new search method from repository
            var (bookings, totalCount) = await _bookingRepository.SearchBookingsAsync(
                request.ProviderId,
                request.CustomerId,
                request.ServiceId,
                request.StaffId,
                request.Status,
                request.StartDate,
                request.EndDate,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var bookingDtos = bookings.Select(b => new BookingSearchDto(
                BookingId: b.Id.Value,
                CustomerId: b.CustomerId.Value,
                ProviderId: b.ProviderId.Value,
                ServiceId: b.ServiceId.Value,
                StaffId: b.StaffId,
                StartTime: b.TimeSlot.StartTime,
                EndTime: b.TimeSlot.EndTime,
                Status: b.Status.ToString(),
                TotalPrice: b.TotalPrice.Amount,
                Currency: b.TotalPrice.Currency,
                RequestedAt: b.RequestedAt,
                ConfirmedAt: b.ConfirmedAt,
                CustomerNotes: b.CustomerNotes
            )).ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            _logger.LogInformation("Found {Count} bookings matching criteria", totalCount);

            return new SearchBookingsResult(
                Bookings: bookingDtos,
                TotalCount: totalCount,
                PageNumber: request.PageNumber,
                PageSize: request.PageSize,
                TotalPages: totalPages);
        }
    }
}
