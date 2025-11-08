namespace Booksy.Core.Domain.Patterns.Memento;

/// <summary>
/// Base interface for all memento objects that capture state snapshots.
/// Follows the Gang of Four Memento pattern for capturing and restoring object state.
/// </summary>
public interface IMemento
{
    /// <summary>
    /// Unique identifier for this state snapshot.
    /// </summary>
    Guid StateId { get; }

    /// <summary>
    /// Timestamp when this snapshot was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Descriptive name of the state (e.g., "Confirmed", "Cancelled").
    /// </summary>
    string StateName { get; }
}

/// <summary>
/// Generic interface for strongly-typed memento objects that capture specific state.
/// </summary>
/// <typeparam name="TState">The type of state being captured.</typeparam>
public interface IMemento<out TState> : IMemento
{
    /// <summary>
    /// The captured state snapshot.
    /// </summary>
    TState State { get; }
}
