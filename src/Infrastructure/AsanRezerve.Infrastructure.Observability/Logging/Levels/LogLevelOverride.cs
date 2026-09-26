using Microsoft.Extensions.Logging;

namespace AsanRezerve.Infrastructure.Observability.Logging.Levels;

/// <summary>An admin's override of one category's level; <see cref="ExpiresAt"/> null means until reset.</summary>
public sealed record LogLevelOverride(
    string Category,
    LogLevel Level,
    DateTimeOffset? ExpiresAt,
    string UpdatedBy,
    DateTimeOffset UpdatedAt);

/// <summary>One row of the admin log-levels page.</summary>
/// <param name="ConfiguredLevel">What configuration says for exactly this category; null when it inherits.</param>
/// <param name="EffectiveLevel">What applies now, overrides and inheritance included.</param>
public sealed record LogLevelInfo(
    string Category,
    LogLevel? ConfiguredLevel,
    LogLevel EffectiveLevel,
    LogLevelOverride? Override);

/// <summary>Where overrides survive a restart.</summary>
public interface ILogLevelOverrideStore
{
    Task<IReadOnlyList<LogLevelOverride>> LoadAsync(CancellationToken cancellationToken = default);

    Task UpsertAsync(LogLevelOverride value, CancellationToken cancellationToken = default);

    Task DeleteAsync(string category, CancellationToken cancellationToken = default);
}

/// <summary>Process-local store: overrides do not survive a restart. Used when the database store is off.</summary>
public sealed class InMemoryLogLevelOverrideStore : ILogLevelOverrideStore
{
    private readonly Dictionary<string, LogLevelOverride> _items = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<LogLevelOverride> Items
    {
        get
        {
            lock (_items) return _items.Values.ToList();
        }
    }

    public Task<IReadOnlyList<LogLevelOverride>> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Items);

    public Task UpsertAsync(LogLevelOverride value, CancellationToken cancellationToken = default)
    {
        lock (_items) _items[value.Category] = value;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string category, CancellationToken cancellationToken = default)
    {
        lock (_items) _items.Remove(category);
        return Task.CompletedTask;
    }
}
