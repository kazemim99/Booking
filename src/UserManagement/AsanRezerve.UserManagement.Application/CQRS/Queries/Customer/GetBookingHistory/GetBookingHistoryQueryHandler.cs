// ========================================
// AsanRezerve.UserManagement.Application/CQRS/Queries/Customer/GetBookingHistory/GetBookingHistoryQueryHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.UserManagement.Application.CQRS.Queries.Customer.GetBookingHistory
{
    /// <summary>
    /// One page of the customer's booking history, most recent first, from the read model that
    /// <c>BookingEventSubscribers</c> maintains. (This was a stub that always returned an empty
    /// page with TotalCount 0.)
    /// </summary>
    public sealed class GetBookingHistoryQueryHandler
        : IQueryHandler<GetBookingHistoryQuery, BookingHistoryResult>
    {
        private readonly ICustomerBookingHistoryReadRepository _history;
        private readonly ILogger<GetBookingHistoryQueryHandler> _logger;

        public GetBookingHistoryQueryHandler(
            ICustomerBookingHistoryReadRepository history,
            ILogger<GetBookingHistoryQueryHandler> logger)
        {
            _history = history;
            _logger = logger;
        }

        public async Task<BookingHistoryResult> Handle(
            GetBookingHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var (items, total) = await _history.GetHistoryPageAsync(
                request.CustomerId, request.Page, request.PageSize, cancellationToken);

            _logger.LogInformation(
                "Found {Count} total bookings for CustomerId: {CustomerId} (page {Page}, size {PageSize})",
                total, request.CustomerId, request.Page, request.PageSize);

            return new BookingHistoryResult
            {
                Items = items.Select(e => new BookingHistoryViewModel
                {
                    BookingId = e.BookingId,
                    ProviderId = e.ProviderId,
                    ProviderName = e.ProviderName,
                    ServiceName = e.ServiceName,
                    StartTime = e.StartTime,
                    Status = e.Status,
                    TotalPrice = e.TotalPrice,
                    CreatedAt = e.CreatedAt,
                }).ToList(),
                TotalCount = total,
                Page = request.Page,
                PageSize = request.PageSize,
            };
        }
    }
}
