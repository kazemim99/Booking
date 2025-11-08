namespace Booksy.Core.Domain.Patterns.Memento;

/// <summary>
/// Interface for objects that can create and restore from mementos.
/// Follows the Gang of Four Memento pattern for state management.
/// </summary>
/// <typeparam name="TMemento">The type of memento this originator creates.</typeparam>
public interface IOriginator<TMemento> where TMemento : IMemento
{
    /// <summary>
    /// Creates a memento capturing the current state of the originator.
    /// </summary>
    /// <param name="stateName">Descriptive name for the current state.</param>
    /// <returns>A memento containing the current state snapshot.</returns>
    TMemento CreateMemento(string stateName);

    /// <summary>
    /// Restores the originator's state from a memento.
    /// </summary>
    /// <param name="memento">The memento containing the state to restore.</param>
    void RestoreFromMemento(TMemento memento);
}
