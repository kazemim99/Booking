using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace AsanRezerve.Infrastructure.Observability.Diagnostics;

/// <summary>A counter's total since the process started.</summary>
public sealed record CounterTotal(string Meter, string Name, string? Description, long Total);

/// <summary>
/// Totals of every counter the application's own meters (<c>AsanRezerve.*</c>) publish — bookings created,
/// confirmed, cancelled…, cache reads. <c>BookingMetrics</c> counted into the void until now: nothing listened to
/// its meter (the OpenTelemetry wiring lived in a project no one referenced).
/// </summary>
public sealed class ApplicationCounters : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly ConcurrentDictionary<Instrument, long[]> _totals = new();

    public ApplicationCounters()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name.StartsWith("AsanRezerve", StringComparison.Ordinal)
                && instrument is Counter<long> or Counter<int> or UpDownCounter<long> or UpDownCounter<int>)
            {
                _totals.TryAdd(instrument, [0]);
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, _, _) => Add(instrument, value));
        _listener.SetMeasurementEventCallback<int>((instrument, value, _, _) => Add(instrument, value));
        _listener.Start();
    }

    private void Add(Instrument instrument, long value)
    {
        if (_totals.TryGetValue(instrument, out var total))
            Interlocked.Add(ref total[0], value);
    }

    public IReadOnlyList<CounterTotal> Snapshot() =>
        _totals
            .Select(kv => new CounterTotal(kv.Key.Meter.Name, kv.Key.Name, kv.Key.Description, Interlocked.Read(ref kv.Value[0])))
            .OrderBy(c => c.Meter, StringComparer.Ordinal).ThenBy(c => c.Name, StringComparer.Ordinal)
            .ToList();

    public void Dispose() => _listener.Dispose();
}
