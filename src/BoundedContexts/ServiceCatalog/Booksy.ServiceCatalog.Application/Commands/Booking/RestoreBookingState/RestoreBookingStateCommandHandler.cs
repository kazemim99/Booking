// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/RestoreBookingState/RestoreBookingStateCommandHandler.cs
// ========================================
using System.Text.Json;
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.RestoreBookingState;

/// <summary>
/// Handler for restoring a booking to a previous state from audit history.
/// This is an admin-only operation for error correction.
/// </summary>
public sealed class RestoreBookingStateCommandHandler : ICommandHandler<RestoreBookingStateCommand, RestoreBookingStateResult>
{
    private readonly IBookingWriteRepository _bookingWriteRepository;
    private readonly IBookingReadRepository _bookingReadRepository;
    private readonly ILogger<RestoreBookingStateCommandHandler> _logger;

    public RestoreBookingStateCommandHandler(
        IBookingWriteRepository bookingWriteRepository,
        IBookingReadRepository bookingReadRepository,
        ILogger<RestoreBookingStateCommandHandler> logger)
    {
        _bookingWriteRepository = bookingWriteRepository;
        _bookingReadRepository = bookingReadRepository;
        _logger = logger;
    }

    public async Task<RestoreBookingStateResult> Handle(
        RestoreBookingStateCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Restoring booking {BookingId} to state {StateId} by {User}. Reason: {Reason}",
            request.BookingId, request.StateId, request.RestoredBy ?? "System", request.Reason ?? "Not specified");

        try
        {
            var bookingId = BookingId.Create(request.BookingId);

            // Load the current booking
            var booking = await _bookingWriteRepository.GetByIdAsync(bookingId, cancellationToken);
            if (booking == null)
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            var previousStatus = booking.Status.ToString();

            // Load the historical snapshot
            var snapshot = await _bookingReadRepository.GetBookingHistorySnapshotByStateIdAsync(
                bookingId,
                request.StateId,
                cancellationToken);

            if (snapshot == null)
                throw new NotFoundException(
                    $"Historical state with ID {request.StateId} not found for booking {request.BookingId}");

            // Deserialize the state
            var bookingState = JsonSerializer.Deserialize<BookingState>(
                snapshot.StateJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (bookingState == null)
                throw new InvalidOperationException("Failed to deserialize booking state from snapshot");

            // Create memento from the snapshot
            var memento = BookingMemento.Create(
                booking,
                snapshot.StateName,
                request.RestoredBy,
                $"Restored from state: {snapshot.StateName}. Reason: {request.Reason}");

            // Restore the booking state
            booking.RestoreFromMemento(memento);

            // Update booking in repository
            await _bookingWriteRepository.UpdateBookingAsync(booking, cancellationToken);

            var restoredStatus = booking.Status.ToString();

            _logger.LogWarning("Successfully restored booking {BookingId} from {PreviousStatus} to {RestoredStatus}",
                request.BookingId, previousStatus, restoredStatus);

            return new RestoreBookingStateResult(
                BookingId: request.BookingId,
                RestoredStateId: request.StateId,
                PreviousStatus: previousStatus,
                RestoredStatus: restoredStatus,
                RestoredAt: DateTime.UtcNow,
                Success: true,
                Message: $"Booking restored to state: {snapshot.StateName}");
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring booking {BookingId} to state {StateId}",
                request.BookingId, request.StateId);

            return new RestoreBookingStateResult(
                BookingId: request.BookingId,
                RestoredStateId: request.StateId,
                PreviousStatus: "Unknown",
                RestoredStatus: "Unknown",
                RestoredAt: DateTime.UtcNow,
                Success: false,
                Message: $"Failed to restore booking: {ex.Message}");
        }
    }
}
