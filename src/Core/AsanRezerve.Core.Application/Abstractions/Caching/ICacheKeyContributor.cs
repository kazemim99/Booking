namespace AsanRezerve.Core.Application.Abstractions.Caching;

/// <summary>
/// Something outside the query that a cached result depends on — for example the public base URL that photo links
/// are made absolute with, when it comes from the request's host rather than configuration. Its value is added to
/// every query-cache key, so each variant gets its own entry.
/// <para>Keep the values few and stable (a host name, a culture); anything per user does not belong in a cached
/// query at all.</para>
/// </summary>
public interface ICacheKeyContributor
{
    /// <summary>The variant to add to the key, or null/empty for none.</summary>
    string? Contribute();
}
