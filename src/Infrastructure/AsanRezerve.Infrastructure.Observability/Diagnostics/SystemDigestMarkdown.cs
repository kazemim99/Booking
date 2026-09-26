using System.Globalization;
using System.Text;

namespace AsanRezerve.Infrastructure.Observability.Diagnostics;

/// <summary>
/// Renders a <see cref="SystemDigest"/> as Markdown: short, tabular, and self-describing, so it can be pasted into
/// (or fetched by) an AI assistant as-is. Messages are already masked; nothing here adds personal data.
/// </summary>
public static class SystemDigestMarkdown
{
    public static string Render(SystemDigest digest)
    {
        var md = new StringBuilder();
        md.AppendLine($"# AsanRezerve system digest");
        md.AppendLine();
        md.AppendLine($"Window: {Utc(digest.From)} → {Utc(digest.To)} (generated {Utc(digest.GeneratedAt)}). " +
                      "Log messages are masked (phones, e-mails, codes, tokens). Trace ids can be opened in the admin Logs page.");
        md.AppendLine();

        md.AppendLine("## Events by level");
        md.AppendLine();
        if (digest.Levels.Count == 0)
        {
            md.AppendLine("No stored events in this window.");
        }
        else
        {
            md.AppendLine("| Level | Count |");
            md.AppendLine("|---|---:|");
            foreach (var level in digest.Levels)
                md.AppendLine($"| {Cell(level.Level)} | {level.Count} |");
        }
        md.AppendLine();

        md.AppendLine("## Warnings and errors, grouped");
        md.AppendLine();
        if (digest.ErrorGroups.Count == 0)
        {
            md.AppendLine("No warnings or errors in this window.");
        }
        else
        {
            md.AppendLine("| Level | Count | First seen | Last seen | Source | Message template | Exception | Sample trace ids |");
            md.AppendLine("|---|---:|---|---|---|---|---|---|");
            foreach (var group in digest.ErrorGroups)
            {
                md.AppendLine(
                    $"| {Cell(group.Level)} | {group.Count} | {Utc(group.FirstSeen)} | {Utc(group.LastSeen)} | " +
                    $"{Cell(group.SourceContext)} | {Cell(group.MessageTemplate)} | {Cell(group.ExceptionType)} | " +
                    $"{Cell(string.Join(", ", group.SampleTraceIds))} |");
            }
        }
        md.AppendLine();

        md.AppendLine("## Slowest routes");
        md.AppendLine();
        if (digest.SlowestRoutes.Count == 0)
        {
            md.AppendLine("No request events in this window.");
        }
        else
        {
            md.AppendLine("| Route | Requests | 5xx | p50 ms | p95 ms | max ms |");
            md.AppendLine("|---|---:|---:|---:|---:|---:|");
            foreach (var route in digest.SlowestRoutes)
            {
                md.AppendLine($"| {Cell(route.Route)} | {route.Requests} | {route.ServerErrors} | {Ms(route.P50Ms)} | {Ms(route.P95Ms)} | {Ms(route.MaxMs)} |");
            }
        }
        md.AppendLine();

        md.AppendLine("## Cache (since the process started)");
        md.AppendLine();
        if (digest.Cache.Count == 0)
        {
            md.AppendLine("No cached reads yet.");
        }
        else
        {
            md.AppendLine("| Region | Requests | Hit ratio |");
            md.AppendLine("|---|---:|---:|");
            foreach (var region in digest.Cache)
                md.AppendLine($"| {Cell(region.Region)} | {region.Requests} | {Math.Round(region.HitRatio * 100).ToString(CultureInfo.InvariantCulture)}% |");
        }

        if (digest.LogStore is { } store)
        {
            md.AppendLine();
            md.AppendLine("## Log store");
            md.AppendLine();
            md.AppendLine($"Written {store.Written}, dropped {store.Dropped}, failed batches {store.FailedBatches}, " +
                          $"queued {store.QueueLength}{(store.LastError is null ? "" : $", last error: {Cell(store.LastError)}")}.");
        }

        return md.ToString();
    }

    private static string Utc(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string Ms(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>A table cell: pipes escaped, line breaks flattened, long text shortened.</summary>
    private static string Cell(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "—";
        var flat = value.Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|");
        return flat.Length <= 160 ? flat : flat[..157] + "...";
    }
}
