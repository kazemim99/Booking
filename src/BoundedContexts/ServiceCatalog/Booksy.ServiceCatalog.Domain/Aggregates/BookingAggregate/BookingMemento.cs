using Booksy.Core.Domain.Patterns.Memento;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;

/// <summary>
/// Immutable snapshot of booking state for the Memento pattern.
/// Captures all essential properties for complete state restoration.
/// </summary>
public sealed record BookingState
{
    // Core Identity
    public BookingId BookingId { get; init; }
    public UserId CustomerId { get; init; }
    public ProviderId ProviderId { get; init; }
    public ServiceId ServiceId { get; init; }
    public Guid StaffId { get; init; }

    // Booking Details
    public TimeSlot TimeSlot { get; init; }
    public Duration Duration { get; init; }
    public BookingStatus Status { get; init; }

    // Pricing & Payment
    public Price TotalPrice { get; init; }
    public PaymentInfo PaymentInfo { get; init; }

    // Policy & Rules
    public BookingPolicy Policy { get; init; }

    // Additional Information
    public string? CustomerNotes { get; init; }
    public string? StaffNotes { get; init; }
    public string? CancellationReason { get; init; }

    // Timestamps
    public DateTime RequestedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? RescheduledAt { get; init; }

    // Rescheduling Links
    public BookingId? PreviousBookingId { get; init; }
    public BookingId? RescheduledToBookingId { get; init; }

    // Audit Properties
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? LastModifiedAt { get; init; }
    public string? LastModifiedBy { get; init; }

    private BookingState() { }

    /// <summary>
    /// Creates a BookingState from a Booking aggregate.
    /// </summary>
    public static BookingState FromBooking(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        return new BookingState
        {
            BookingId = booking.Id,
            CustomerId = booking.CustomerId,
            ProviderId = booking.ProviderId,
            ServiceId = booking.ServiceId,
            StaffId = booking.StaffId,
            TimeSlot = booking.TimeSlot,
            Duration = booking.Duration,
            Status = booking.Status,
            TotalPrice = booking.TotalPrice,
            PaymentInfo = booking.PaymentInfo,
            Policy = booking.Policy,
            CustomerNotes = booking.CustomerNotes,
            StaffNotes = booking.StaffNotes,
            CancellationReason = booking.CancellationReason,
            RequestedAt = booking.RequestedAt,
            ConfirmedAt = booking.ConfirmedAt,
            CancelledAt = booking.CancelledAt,
            CompletedAt = booking.CompletedAt,
            RescheduledAt = booking.RescheduledAt,
            PreviousBookingId = booking.PreviousBookingId,
            RescheduledToBookingId = booking.RescheduledToBookingId,
            CreatedAt = booking.CreatedAt,
            CreatedBy = booking.CreatedBy,
            LastModifiedAt = booking.LastModifiedAt,
            LastModifiedBy = booking.LastModifiedBy
        };
    }
}

/// <summary>
/// Memento for capturing and restoring Booking aggregate state.
/// Implements the Gang of Four Memento pattern.
/// </summary>
public sealed class BookingMemento : IMemento<BookingState>
{
    /// <inheritdoc />
    public Guid StateId { get; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; }

    /// <inheritdoc />
    public string StateName { get; }

    /// <inheritdoc />
    public BookingState State { get; }

    /// <summary>
    /// User who triggered the state change.
    /// </summary>
    public string? TriggeredBy { get; }

    /// <summary>
    /// Optional description of why this snapshot was created.
    /// </summary>
    public string? Description { get; }

    private BookingMemento(
        BookingState state,
        string stateName,
        string? triggeredBy = null,
        string? description = null)
    {
        StateId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        State = state;
        StateName = stateName;
        TriggeredBy = triggeredBy;
        Description = description;
    }

    /// <summary>
    /// Creates a memento from a booking aggregate.
    /// </summary>
    public static BookingMemento Create(
        Booking booking,
        string stateName,
        string? triggeredBy = null,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);

        var state = BookingState.FromBooking(booking);
        return new BookingMemento(state, stateName, triggeredBy, description);
    }

}
