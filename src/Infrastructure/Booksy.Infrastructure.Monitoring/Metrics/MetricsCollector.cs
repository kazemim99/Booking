// ========================================
// Metrics/MetricsCollector.cs
// ========================================
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Monitoring.Metrics;

/// <summary>
/// Default implementation of metrics collector
/// </summary>
public sealed class MetricsCollector : IMetricsCollector
{
    private readonly Meter _meter;
    private readonly ILogger<MetricsCollector> _logger;
    private readonly Dictionary<string, Counter<double>> _counters = new();
    private readonly Dictionary<string, Histogram<double>> _histograms = new();

    public MetricsCollector(ILogger<MetricsCollector> logger)
    {
        _logger = logger;
        _meter = new Meter("Booksy.Application", "1.0.0");
    }

    public void IncrementCounter(string name, double value = 1, Dictionary<string, object>? tags = null)
    {
        try
        {
            if (!_counters.TryGetValue(name, out var counter))
            {
                counter = _meter.CreateCounter<double>(name);
                _counters[name] = counter;
            }

            if (tags != null)
            {
                var tagList = CreateTagList(tags);
                counter.Add(value, tagList);
            }
            else
            {
                counter.Add(value);
            }

            _logger.LogTrace("Counter {Name} incremented by {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment counter {Name}", name);
        }
    }

    public void RecordGauge(string name, double value, Dictionary<string, object>? tags = null)
    {
        try
        {
            var gauge = _meter.CreateObservableGauge(name, () => value);
            _logger.LogTrace("Gauge {Name} recorded with value {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record gauge {Name}", name);
        }
    }

    public void RecordHistogram(string name, double value, Dictionary<string, object>? tags = null)
    {
        try
        {
            if (!_histograms.TryGetValue(name, out var histogram))
            {
                histogram = _meter.CreateHistogram<double>(name);
                _histograms[name] = histogram;
            }

            if (tags != null)
            {
                var tagList = CreateTagList(tags);
                histogram.Record(value, tagList);
            }
            else
            {
                histogram.Record(value);
            }

            _logger.LogTrace("Histogram {Name} recorded value {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record histogram {Name}", name);
        }
    }

    public IDisposable StartTimer(string name, Dictionary<string, object>? tags = null)
    {
        return new TimerScope(this, name, tags);
    }

    public void RecordEvent(string name, string description, Dictionary<string, object>? tags = null)
    {
        try
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.AddEvent(new ActivityEvent(name));

                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        activity.SetTag(tag.Key, tag.Value);
                    }
                }
            }

            _logger.LogInformation("Event {EventName}: {Description}", name, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record event {Name}", name);
        }
    }

    private static TagList CreateTagList(Dictionary<string, object> tags)
    {
        var tagList = new TagList();
        foreach (var tag in tags)
        {
            tagList.Add(tag.Key, tag.Value);
        }
        return tagList;
    }

    private sealed class TimerScope : IDisposable
    {
        private readonly MetricsCollector _collector;
        private readonly string _name;
        private readonly Dictionary<string, object>? _tags;
        private readonly Stopwatch _stopwatch;

        public TimerScope(MetricsCollector collector, string name, Dictionary<string, object>? tags)
        {
            _collector = collector;
            _name = name;
            _tags = tags;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _collector.RecordHistogram($"{_name}.duration", _stopwatch.ElapsedMilliseconds, _tags);
        }
    }
}