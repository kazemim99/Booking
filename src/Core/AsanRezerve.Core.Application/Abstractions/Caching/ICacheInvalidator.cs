namespace AsanRezerve.Core.Application.Abstractions.Caching;

/// <summary>
/// Evicts cached reads by tag when the data behind them changes.
/// <para>Eviction happens twice: when asked, and again when the current DI scope (the request, the job) ends —
/// after its unit of work committed. Domain events are dispatched before <c>SaveChanges</c>, so a read that runs
/// between the first eviction and the commit could otherwise cache the old row again.</para>
/// <para>Never throws: a failed eviction is logged, and the entry still expires on its own lifetime.</para>
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>Evicts every cached entry carrying any of <paramref name="tags"/>.</summary>
    Task InvalidateAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);

    /// <summary>Evicts every cached entry.</summary>
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);
}
