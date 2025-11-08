using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingHistory;

/// <summary>
/// Handler for getting complete booking history (state snapshots).
/// </summary>
public sealed class GetBookingHistoryQueryHandler : IQueryHandler<GetBookingHistoryQuery, IReadOnlyList<BookingHistorySnapshotDto>>
{
    private readonly IBookingReadRepository _bookingRepository;
    private readonly ILogger<GetBookingHistoryQueryHandler> _logger;

    public GetBookingHistoryQueryHandler(
        IBookingReadRepository bookingRepository,
        ILogger<GetBookingHistoryQueryHandler> logger)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BookingHistorySnapshotDto>> Handle(
        GetBookingHistoryQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting booking history for booking ID: {BookingId}", request.BookingId);

        try
        {
            var bookingId = BookingId.Create(request.BookingId);
            var snapshots = await _bookingRepository.GetBookingHistorySnapshotsAsync(
                bookingId,
                cancellationToken);

            var result = snapshots
                .OrderBy(s => s.CreatedAt)
                .Select(s => new BookingHistorySnapshotDto(
                    s.Id,
                    s.StateId,
                    s.StateName,
                    s.StateJson,
                    s.CreatedAt,
                    s.TriggeredBy,
                    s.Description))
                .ToList();

            _logger.LogInformation("Retrieved {Count} history snapshots for booking {BookingId}",
                result.Count, request.BookingId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking history for booking ID: {BookingId}",
                request.BookingId);
            throw;
        }
    }
}
