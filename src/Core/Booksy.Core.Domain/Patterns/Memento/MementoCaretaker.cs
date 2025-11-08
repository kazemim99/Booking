namespace Booksy.Core.Domain.Patterns.Memento;

/// <summary>
/// Manages the history of mementos for an originator.
/// Follows the Gang of Four Memento pattern as the Caretaker role.
/// </summary>
/// <typeparam name="TMemento">The type of memento being managed.</typeparam>
public sealed class MementoCaretaker<TMemento> where TMemento : IMemento
{
    private readonly List<TMemento> _history = new();

    /// <summary>
    /// Saves a memento to the history.
    /// </summary>
    /// <param name="memento">The memento to save.</param>
    public void SaveMemento(TMemento memento)
    {
        ArgumentNullException.ThrowIfNull(memento);
        _history.Add(memento);
    }

    /// <summary>
    /// Retrieves a specific memento by its state ID.
    /// </summary>
    /// <param name="stateId">The unique identifier of the state.</param>
    /// <returns>The memento if found; otherwise, null.</returns>
    public TMemento? GetMemento(Guid stateId)
    {
        return _history.FirstOrDefault(m => m.StateId == stateId);
    }

    /// <summary>
    /// Gets the most recently saved memento.
    /// </summary>
    /// <returns>The latest memento if history exists; otherwise, null.</returns>
    public TMemento? GetLatestMemento()
    {
        return _history.Count > 0 ? _history[^1] : default;
    }

    /// <summary>
    /// Gets all mementos in chronological order.
    /// </summary>
    /// <returns>A read-only list of all mementos.</returns>
    public IReadOnlyList<TMemento> GetAllMementos()
    {
        return _history.AsReadOnly();
    }

    /// <summary>
    /// Gets the count of mementos in the history.
    /// </summary>
    public int Count => _history.Count;

    /// <summary>
    /// Clears all mementos from the history.
    /// </summary>
    public void Clear()
    {
        _history.Clear();
    }

    /// <summary>
    /// Gets mementos created within a specific time range.
    /// </summary>
    /// <param name="fromDate">Start date (inclusive).</param>
    /// <param name="toDate">End date (inclusive).</param>
    /// <returns>List of mementos within the date range.</returns>
    public IReadOnlyList<TMemento> GetMementosInRange(DateTime fromDate, DateTime toDate)
    {
        return _history
            .Where(m => m.CreatedAt >= fromDate && m.CreatedAt <= toDate)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets the memento created immediately before the specified state.
    /// </summary>
    /// <param name="stateId">The state ID to find the previous state for.</param>
    /// <returns>The previous memento if found; otherwise, null.</returns>
    public TMemento? GetPreviousMemento(Guid stateId)
    {
        var index = _history.FindIndex(m => m.StateId == stateId);
        if (index > 0)
        {
            return _history[index - 1];
        }
        return default;
    }
}
