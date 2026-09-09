// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetUpcomingBookings/GetUpcomingBookingsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.UserManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetUpcomingBookings
{
    /// <summary>
    /// The customer's upcoming bookings, read from the booking history read model that
    /// <c>BookingEventSubscribers</c> maintains from ServiceCatalog booking events. (This was a
    /// stub that always returned an empty list, so the endpoint never showed anything.)
    /// </summary>
    public sealed class GetUpcomingBookingsQueryHandler
        : IQueryHandler<GetUpcomingBookingsQuery, List<UpcomingBookingViewModel>>
    {
        private readonly ICustomerBookingHistoryReadRepository _history;
        private readonly IDateTimeProvider _clock;
        private readonly ILogger<GetUpcomingBookingsQueryHandler> _logger;

        public GetUpcomingBookingsQueryHandler(
            ICustomerBookingHistoryReadRepository history,
            IDateTimeProvider clock,
            ILogger<GetUpcomingBookingsQueryHandler> logger)
        {
            _history = history;
            _clock = clock;
            _logger = logger;
        }

        public async Task<List<UpcomingBookingViewModel>> Handle(
            GetUpcomingBookingsQuery request,
            CancellationToken cancellationToken)
        {
            var entries = await _history.GetUpcomingAsync(
                request.CustomerId, _clock.UtcNow, request.Limit, cancellationToken);

            _logger.LogInformation(
                "Found {Count} upcoming bookings for CustomerId: {CustomerId}",
                entries.Count, request.CustomerId);

            return entries
                .Select(e => new UpcomingBookingViewModel
                {
                    BookingId = e.BookingId,
                    ProviderId = e.ProviderId,
                    ProviderName = e.ProviderName,
                    ServiceName = e.ServiceName,
                    StartTime = e.StartTime,
                    Status = e.Status,
                    TotalPrice = e.TotalPrice,
                })
                .ToList();
        }
    }
}
