using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate.Entities;

/// <summary>
/// Entity for persisting booking state snapshots to the database.
/// Stores immutable memento data for audit trail and state restoration.
/// </summary>
public sealed class BookingHistorySnapshot : Entity<Guid>
{
    /// <summary>
    /// The booking this snapshot belongs to.
    /// </summary>
    public BookingId BookingId { get; private set; }

    /// <summary>
    /// Unique identifier for this state snapshot (matches memento StateId).
    /// </summary>
    public Guid StateId { get; private set; }

    /// <summary>
    /// Descriptive name of the state (e.g., "Before Confirm - Requested").
    /// </summary>
    public string StateName { get; private set; }

    /// <summary>
    /// JSON-serialized snapshot of the booking state.
    /// </summary>
    public string StateJson { get; private set; }

    /// <summary>
    /// Timestamp when this snapshot was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// User who triggered the state change that created this snapshot.
    /// </summary>
    public string? TriggeredBy { get; private set; }

    /// <summary>
    /// Optional description of why this snapshot was created.
    /// </summary>
    public string? Description { get; private set; }

    // Private parameterless constructor for EF Core
    private BookingHistorySnapshot() : base()
    {
        StateName = string.Empty;
        StateJson = string.Empty;
    }

    /// <summary>
    /// Creates a new booking history snapshot.
    /// </summary>
    private BookingHistorySnapshot(
        BookingId bookingId,
        Guid stateId,
        string stateName,
        string stateJson,
        DateTime createdAt,
        string? triggeredBy = null,
        string? description = null) : base()
    {
        Id = Guid.NewGuid();
        BookingId = bookingId;
        StateId = stateId;
        StateName = stateName;
        StateJson = stateJson;
        CreatedAt = createdAt;
        TriggeredBy = triggeredBy;
        Description = description;
    }

    /// <summary>
    /// Creates a booking history snapshot from a memento.
    /// </summary>
    /// <param name="bookingId">The booking ID this snapshot belongs to.</param>
    /// <param name="memento">The memento to persist.</param>
    /// <param name="stateJson">JSON-serialized state from the memento.</param>
    /// <returns>A new BookingHistorySnapshot entity.</returns>
    public static BookingHistorySnapshot Create(
        BookingId bookingId,
        BookingMemento memento,
        string stateJson)
    {
        ArgumentNullException.ThrowIfNull(bookingId);
        ArgumentNullException.ThrowIfNull(memento);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateJson);

        return new BookingHistorySnapshot(
            bookingId,
            memento.StateId,
            memento.StateName,
            stateJson,
            memento.CreatedAt,
            memento.TriggeredBy,
            memento.Description);
    }

    /// <summary>
    /// Creates a booking history snapshot with explicit values.
    /// </summary>
    public static BookingHistorySnapshot Create(
        BookingId bookingId,
        Guid stateId,
        string stateName,
        string stateJson,
        DateTime createdAt,
        string? triggeredBy = null,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(bookingId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateJson);

        return new BookingHistorySnapshot(
            bookingId,
            stateId,
            stateName,
            stateJson,
            createdAt,
            triggeredBy,
            description);
    }
}
