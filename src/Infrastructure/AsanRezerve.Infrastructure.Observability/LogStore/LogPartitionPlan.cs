using System.Globalization;

namespace AsanRezerve.Infrastructure.Observability.LogStore;

/// <summary>
/// The daily partitions of <c>observability.log_events</c> (design D6): one per UTC day, named
/// <c>log_events_pYYYYMMDD</c>. Yesterday, today and the next two days always exist, so a late or early event and a
/// maintenance run that misses a day still land in a day partition. A day is dropped only once all of it is older
/// than the retention: dropping a partition is instant and leaves no dead rows behind, unlike <c>DELETE</c>.
/// </summary>
public static class LogPartitionPlan
{
    public const string Prefix = "log_events_p";

    public const string DefaultPartition = "log_events_default";

    private const int DaysBehind = 1;
    private const int DaysAhead = 2;

    public static string NameOf(DateOnly day) => Prefix + day.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    /// <summary>The day a partition holds, or false for any other table (the default partition included).</summary>
    public static bool TryParseDay(string name, out DateOnly day)
    {
        day = default;
        return name.Length == Prefix.Length + 8
               && name.StartsWith(Prefix, StringComparison.Ordinal)
               && DateOnly.TryParseExact(name.AsSpan(Prefix.Length), "yyyyMMdd", CultureInfo.InvariantCulture,
                   DateTimeStyles.None, out day);
    }

    /// <summary>[start of the day, start of the next day), in UTC.</summary>
    public static (DateTimeOffset From, DateTimeOffset To) Bounds(DateOnly day)
    {
        var from = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return (from, from.AddDays(1));
    }

    /// <summary>The days around <paramref name="now"/> that have no partition yet, oldest first.</summary>
    public static IReadOnlyList<DateOnly> ToCreate(DateTimeOffset now, IEnumerable<string> existing)
    {
        var have = Days(existing).ToHashSet();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        return Enumerable.Range(-DaysBehind, DaysBehind + DaysAhead + 1)
            .Select(today.AddDays)
            .Where(day => !have.Contains(day))
            .ToList();
    }

    /// <summary>The partitions whose whole day ended before <paramref name="now"/> minus the retention, oldest first.</summary>
    public static IReadOnlyList<string> ToDrop(DateTimeOffset now, IEnumerable<string> existing, int retentionDays)
    {
        var cutoff = Cutoff(now, retentionDays);
        return Days(existing)
            .Where(day => Bounds(day).To <= cutoff)
            .Order()
            .Select(NameOf)
            .ToList();
    }

    /// <summary>Events older than this are past the retention (at least one day).</summary>
    public static DateTimeOffset Cutoff(DateTimeOffset now, int retentionDays) =>
        now.ToUniversalTime() - TimeSpan.FromDays(Math.Max(1, retentionDays));

    private static IEnumerable<DateOnly> Days(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (TryParseDay(name, out var day)) yield return day;
        }
    }
}
