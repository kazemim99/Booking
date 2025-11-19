// ========================================
// Booksy.UserManagement.Application/CQRS/Queries/Customer/GetBookingHistory/GetBookingHistoryQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.CQRS.Queries.Customer.GetBookingHistory
{
    /// <summary>
    /// Handler for GetBookingHistoryQuery
    /// </summary>
    public sealed class GetBookingHistoryQueryHandler : IQueryHandler<GetBookingHistoryQuery, BookingHistoryResult>
    {
        private readonly ILogger<GetBookingHistoryQueryHandler> _logger;

        public GetBookingHistoryQueryHandler(
            ILogger<GetBookingHistoryQueryHandler> logger)
        {
            _logger = logger;
        }

        public async Task<BookingHistoryResult> Handle(
            GetBookingHistoryQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Getting booking history for CustomerId: {CustomerId}, Page: {Page}, PageSize: {PageSize}",
                    request.CustomerId, request.Page, request.PageSize);

                // TODO: Implement repository pattern for CustomerBookingHistory
                // For now, return empty result until the event handlers are set up
                var result = new BookingHistoryResult
                {
                    Items = new List<BookingHistoryViewModel>(),
                    TotalCount = 0,
                    Page = request.Page,
                    PageSize = request.PageSize
                };

                _logger.LogInformation(
                    "Found {Count} total bookings for CustomerId: {CustomerId}",
                    result.TotalCount, request.CustomerId);

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking history for CustomerId: {CustomerId}", request.CustomerId);
                throw;
            }
        }
    }
}
