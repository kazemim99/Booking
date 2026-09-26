namespace AsanRezerve.Core.Application.Abstractions.Caching;

/// <summary>Hit/miss counters per cache region (a query type), shown on the admin cache page and exported as metrics.</summary>
public interface ICacheMetrics
{
    /// <summary>Records one cached read of <paramref name="region"/>: a hit, or a miss that ran the query.</summary>
    void Record(string region, bool hit);
}
