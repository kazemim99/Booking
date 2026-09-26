using System.Text;
using AsanRezerve.Infrastructure.Observability.Logging.Masking;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace AsanRezerve.Infrastructure.Observability.LogStore;

/// <summary>One <c>observability.log_events</c> row, built from a (masked) Serilog event on the writer's thread.</summary>
public sealed record LogEventRow(
    DateTimeOffset Timestamp,
    short Level,
    string Message,
    string? MessageTemplate,
    string? Exception,
    string? SourceContext,
    string? TraceId,
    string? SpanId,
    string? RequestPath,
    string? RouteTemplate,
    int? StatusCode,
    double? ElapsedMs,
    string? UserId,
    string? Properties)
{
    /// <summary>Properties stored as columns (and so left out of the JSON).</summary>
    private static readonly HashSet<string> Promoted = new(StringComparer.Ordinal)
    {
        "SourceContext", "RequestPath", "RouteTemplate", "StatusCode", "ElapsedMs", "UserId",
    };

    private static readonly JsonValueFormatter Json = new(typeTagName: null);

    public static LogEventRow From(LogEvent logEvent) => new(
        logEvent.Timestamp.ToUniversalTime(),
        (short)logEvent.Level,
        // Properties are masked by the enricher; literal text in the message and the exception is scrubbed here.
        Cut(SensitiveDataMaskingEnricher.ScrubText(logEvent.RenderMessage()), 16_000)!,
        Cut(logEvent.MessageTemplate.Text, 4_000),
        Cut(SensitiveDataMaskingEnricher.ScrubText(logEvent.Exception?.ToString()), 32_000),
        Cut(Text(logEvent, "SourceContext"), 300),
        logEvent.TraceId?.ToHexString(),
        logEvent.SpanId?.ToHexString(),
        Cut(Text(logEvent, "RequestPath"), 500),
        Cut(Text(logEvent, "RouteTemplate"), 300),
        Number(logEvent, "StatusCode") is { } status ? (int)status : null,
        Number(logEvent, "ElapsedMs"),
        Cut(Text(logEvent, "UserId"), 100),
        PropertiesJson(logEvent));

    private static string? Text(LogEvent logEvent, string name) =>
        logEvent.Properties.TryGetValue(name, out var value) && value is ScalarValue { Value: { } raw } ? raw.ToString() : null;

    private static double? Number(LogEvent logEvent, string name)
    {
        if (!logEvent.Properties.TryGetValue(name, out var value) || value is not ScalarValue { Value: { } raw }) return null;
        return raw switch
        {
            int i => i,
            long l => l,
            double d => d,
            float f => f,
            decimal m => (double)m,
            short s => s,
            string text when double.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static string? PropertiesJson(LogEvent logEvent)
    {
        var rest = logEvent.Properties.Where(p => !Promoted.Contains(p.Key)).ToList();
        if (rest.Count == 0) return null;

        using var writer = new StringWriter(new StringBuilder(256));
        writer.Write('{');
        var first = true;
        foreach (var (name, value) in rest)
        {
            if (!first) writer.Write(',');
            first = false;
            JsonValueFormatter.WriteQuotedJsonString(name, writer);
            writer.Write(':');
            Json.Format(value, writer);
        }
        writer.Write('}');
        return Cut(writer.ToString(), 64_000) is { } json && json.Length < 64_000 ? json : null;
    }

    private static string? Cut(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}

/// <summary>Writes a batch of rows to the store. Abstracted so the queueing can be tested without a database.</summary>
public interface ILogEventBatchWriter
{
    Task WriteAsync(IReadOnlyList<LogEventRow> rows, CancellationToken cancellationToken);
}
