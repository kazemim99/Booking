// ========================================
// Booksy.ServiceCatalog.Application/Queries/Reports/GetBookingStatistics/GetBookingStatisticsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Visitors;

namespace Booksy.ServiceCatalog.Application.Queries.Reports.GetBookingStatistics
{
    public sealed class GetBookingStatisticsQueryHandler : IQueryHandler<GetBookingStatisticsQuery, BookingStatisticsDto>
    {
        private readonly IBookingReadRepository _bookingRepository;

        public GetBookingStatisticsQueryHandler(IBookingReadRepository bookingRepository)
        {
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
        }

        public async Task<BookingStatisticsDto> Handle(GetBookingStatisticsQuery request, CancellationToken cancellationToken)
        {
            // Create the visitor with filters
            ProviderId? providerId = request.ProviderId.HasValue
                ? ProviderId.From(request.ProviderId.Value)
                : null;

            var visitor = new BookingStatisticsVisitor(providerId, request.StartDate, request.EndDate);

            // Get bookings using search (supports filtering)
            var (bookings, _) = await _bookingRepository.SearchBookingsAsync(
                providerId: request.ProviderId,
                customerId: null,
                serviceId: null,
                staffId: null,
                status: null,
                startDate: request.StartDate,
                endDate: request.EndDate,
                pageNumber: 1,
                pageSize: int.MaxValue, // Get all matching bookings
                cancellationToken: cancellationToken);

            // Visit all bookings
            foreach (var booking in bookings)
            {
                booking.Accept(visitor);
            }

            // Get the result from the visitor
            var result = visitor.GetResult();

            // Map to DTO (in this case it's a direct mapping)
            return new BookingStatisticsDto(
                Total: result.Total,
                Requested: result.Requested,
                Confirmed: result.Confirmed,
                Completed: result.Completed,
                Cancelled: result.Cancelled,
                Rescheduled: result.Rescheduled,
                NoShows: result.NoShows,
                CompletionRate: result.CompletionRate,
                CancellationRate: result.CancellationRate,
                NoShowRate: result.NoShowRate
            );
        }
    }
}
