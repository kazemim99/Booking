using System.Text.Json;
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetBookingAuditTrail;

/// <summary>
/// Handler for getting detailed booking audit trail with parsed state information.
/// </summary>
public sealed class GetBookingAuditTrailQueryHandler : IQueryHandler<GetBookingAuditTrailQuery, IReadOnlyList<BookingAuditTrailEntryDto>>
{
    private readonly IBookingReadRepository _bookingRepository;
    private readonly ILogger<GetBookingAuditTrailQueryHandler> _logger;

    public GetBookingAuditTrailQueryHandler(
        IBookingReadRepository bookingRepository,
        ILogger<GetBookingAuditTrailQueryHandler> logger)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BookingAuditTrailEntryDto>> Handle(
        GetBookingAuditTrailQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting audit trail for booking ID: {BookingId}", request.BookingId);

        try
        {
            var bookingId = BookingId.Create(request.BookingId);
            var snapshots = await _bookingRepository.GetBookingHistorySnapshotsAsync(
                bookingId,
                cancellationToken);

            var result = snapshots
                .OrderBy(s => s.CreatedAt)
                .Select(s =>
                {
                    // Parse the JSON state to extract key information
                    var stateSnapshot = ParseStateSnapshot(s.StateJson);
                    var status = stateSnapshot.GetValueOrDefault("Status", "Unknown");

                    return new BookingAuditTrailEntryDto(
                        s.StateId,
                        s.StateName,
                        status,
                        s.CreatedAt,
                        s.TriggeredBy,
                        s.Description,
                        stateSnapshot);
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} audit trail entries for booking {BookingId}",
                result.Count, request.BookingId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit trail for booking ID: {BookingId}",
                request.BookingId);
            throw;
        }
    }

    private Dictionary<string, string> ParseStateSnapshot(string stateJson)
    {
        try
        {
            var jsonDocument = JsonDocument.Parse(stateJson);
            var stateSnapshot = new Dictionary<string, string>();

            // Extract key properties from the state
            foreach (var property in jsonDocument.RootElement.EnumerateObject())
            {
                // Convert complex objects to string representation
                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? "",
                    JsonValueKind.Number => property.Value.ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => "null",
                    JsonValueKind.Object => property.Value.ToString(),
                    JsonValueKind.Array => property.Value.ToString(),
                    _ => property.Value.ToString()
                };

                stateSnapshot[property.Name] = value;
            }

            return stateSnapshot;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse state snapshot JSON");
            return new Dictionary<string, string>
            {
                { "Error", "Failed to parse state snapshot" },
                { "RawJson", stateJson }
            };
        }
    }
}
