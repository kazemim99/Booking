// ========================================
// Booksy.UserManagement.Infrastructure/EventHandlers/BookingEventSubscribers.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.UserManagement.Domain.ReadModels;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Infrastructure.EventHandlers;

/// <summary>
/// Handles booking-related integration events to maintain customer booking history
/// These events come from the Booking bounded context
/// </summary>
public sealed class BookingEventSubscribers : ICapSubscribe
{
    private readonly UserManagementDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookingEventSubscribers> _logger;

    public BookingEventSubscribers(
        UserManagementDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<BookingEventSubscribers> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles BookingCreatedIntegrationEvent
    /// Creates a new entry in customer_booking_history
    /// </summary>
    /// <remarks>
    /// TODO: This requires the Booking BC to publish BookingCreatedIntegrationEvent
    /// with the following properties: BookingId, CustomerId, ProviderId, ProviderName,
    /// ServiceName, StartTime, TotalPrice
    /// </remarks>
    [CapSubscribe("booksy.booking.created")]
    public async Task HandleBookingCreatedAsync(BookingCreatedIntegrationEvent @event)
    {
        _logger.LogInformation(
            "📨 Received BookingCreatedIntegrationEvent for Booking {BookingId}, Customer {CustomerId}",
            @event.BookingId,
            @event.CustomerId);

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var historyEntry = CustomerBookingHistoryEntry.Create(
                    @event.BookingId,
                    @event.CustomerId,
                    @event.ProviderId,
                    @event.ProviderName,
                    @event.ServiceName,
                    @event.StartTime,
                    "Pending", // Initial status
                    @event.TotalPrice);

                await _dbContext.CustomerBookingHistory.AddAsync(historyEntry);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Created booking history entry for Booking {BookingId}",
                    @event.BookingId);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Error handling BookingCreatedIntegrationEvent for Booking {BookingId}",
                @event.BookingId);
            throw;
        }
    }

    /// <summary>
    /// Handles BookingConfirmedIntegrationEvent
    /// Updates status to "Confirmed"
    /// </summary>
    [CapSubscribe("booksy.booking.confirmed")]
    public async Task HandleBookingConfirmedAsync(BookingStatusChangedIntegrationEvent @event)
    {
        await UpdateBookingStatusAsync(@event.BookingId, "Confirmed");
    }

    /// <summary>
    /// Handles BookingCompletedIntegrationEvent
    /// Updates status to "Completed"
    /// </summary>
    [CapSubscribe("booksy.booking.completed")]
    public async Task HandleBookingCompletedAsync(BookingStatusChangedIntegrationEvent @event)
    {
        await UpdateBookingStatusAsync(@event.BookingId, "Completed");
    }

    /// <summary>
    /// Handles BookingCancelledIntegrationEvent
    /// Updates status to "Cancelled"
    /// </summary>
    [CapSubscribe("booksy.booking.cancelled")]
    public async Task HandleBookingCancelledAsync(BookingStatusChangedIntegrationEvent @event)
    {
        await UpdateBookingStatusAsync(@event.BookingId, "Cancelled");
    }

    private async Task UpdateBookingStatusAsync(Guid bookingId, string newStatus)
    {
        _logger.LogInformation(
            "📨 Received booking status change event for Booking {BookingId}, new status: {Status}",
            bookingId, newStatus);

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var historyEntry = await _dbContext.CustomerBookingHistory
                    .FindAsync(bookingId);

                if (historyEntry != null)
                {
                    historyEntry.UpdateStatus(newStatus);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation(
                        "✅ Updated booking history status for Booking {BookingId} to {Status}",
                        bookingId, newStatus);
                }
                else
                {
                    _logger.LogWarning(
                        "⚠️  Booking history entry not found for Booking {BookingId}",
                        bookingId);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Error updating booking status for Booking {BookingId}",
                bookingId);
            throw;
        }
    }
}

// ========================================
// Integration Event DTOs (placeholders)
// TODO: These should be defined in a shared contracts library
// ========================================

/// <summary>
/// Event published when a new booking is created
/// </summary>
public record BookingCreatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid BookingId,
    Guid CustomerId,
    Guid ProviderId,
    string ProviderName,
    string ServiceName,
    DateTime StartTime,
    decimal? TotalPrice);

/// <summary>
/// Event published when a booking status changes
/// </summary>
public record BookingStatusChangedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid BookingId,
    string NewStatus);
