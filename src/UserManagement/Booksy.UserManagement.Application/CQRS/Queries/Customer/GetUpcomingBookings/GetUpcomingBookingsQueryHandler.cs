// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetUpcomingBookings/GetUpcomingBookingsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetUpcomingBookings
{
    /// <summary>
    /// Handler for GetUpcomingBookingsQuery
    /// </summary>
    public sealed class GetUpcomingBookingsQueryHandler : IQueryHandler<GetUpcomingBookingsQuery, List<UpcomingBookingViewModel>>
    {
        private readonly ILogger<GetUpcomingBookingsQueryHandler> _logger;

        public GetUpcomingBookingsQueryHandler(
            ILogger<GetUpcomingBookingsQueryHandler> logger)
        {
            _logger = logger;
        }

        public async Task<List<UpcomingBookingViewModel>> Handle(
            GetUpcomingBookingsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting upcoming bookings for CustomerId: {CustomerId}", request.CustomerId);

                // TODO: Implement repository pattern for CustomerBookingHistory
                // For now, return empty list until the event handlers are set up
                var bookings = new List<UpcomingBookingViewModel>();

                _logger.LogInformation("Found {Count} upcoming bookings for CustomerId: {CustomerId}",
                    bookings.Count, request.CustomerId);

                return await Task.FromResult(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming bookings for CustomerId: {CustomerId}", request.CustomerId);
                throw;
            }
        }
    }
}
