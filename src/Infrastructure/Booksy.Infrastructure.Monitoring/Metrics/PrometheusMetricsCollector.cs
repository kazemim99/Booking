// ========================================
// Metrics/IMetricsCollector.cs
// ========================================
using Microsoft.Extensions.Logging;
using Prometheus;
using System.Diagnostics.Metrics;
namespace Booksy.Infrastructure.Monitoring.Metrics;


/// <summary>
/// Prometheus implementation of metrics collector
/// </summary>
public sealed class PrometheusMetricsCollector : IMetricsCollector
{
    private readonly ILogger<PrometheusMetricsCollector> _logger;
    private readonly Dictionary<string, Counter> _counters = new();
    private readonly Dictionary<string, Gauge> _gauges = new();
    private readonly Dictionary<string, Histogram> _histograms = new();
    private readonly Dictionary<string, Summary> _summaries = new();

    // Default buckets for typical web application latencies (in seconds)
    private static readonly double[] DefaultLatencyBuckets = { 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 };

    // Default buckets for request sizes (in bytes)
    private static readonly double[] DefaultSizeBuckets = { 100, 1000, 10000, 100000, 1000000, 10000000 };

    public PrometheusMetricsCollector(ILogger<PrometheusMetricsCollector> logger)
    {
        _logger = logger;
    }

    public void IncrementCounter(string name, double value = 1, Dictionary<string, object>? tags = null)
    {
        try
        {
            if (tags != null && tags.Count > 0)
            {
                var counter = GetOrCreateCounter(name, tags);
                var labelValues = tags.Values.Select(v => v.ToString() ?? string.Empty).ToArray();
                counter.WithLabels(labelValues).Inc(value);
            }
            else
            {
                var counter = GetOrCreateCounter(name, null);
                counter.Inc(value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment Prometheus counter {Name}", name);
        }
    }

    public void RecordGauge(string name, double value, Dictionary<string, object>? tags = null)
    {
        try
        {
            if (tags != null && tags.Count > 0)
            {
                var gauge = GetOrCreateGauge(name, tags);
                var labelValues = tags.Values.Select(v => v.ToString() ?? string.Empty).ToArray();
                gauge.WithLabels(labelValues).Set(value);
            }
            else
            {
                var gauge = GetOrCreateGauge(name, null);
                gauge.Set(value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Prometheus gauge {Name}", name);
        }
    }

    public void RecordHistogram(string name, double value, Dictionary<string, object>? tags = null)
    {
        try
        {
            if (tags != null && tags.Count > 0)
            {
                var histogram = GetOrCreateHistogram(name, tags);
                var labelValues = tags.Values.Select(v => v.ToString() ?? string.Empty).ToArray();
                histogram.WithLabels(labelValues).Observe(value);
            }
            else
            {
                var histogram = GetOrCreateHistogram(name, null);
                histogram.Observe(value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Prometheus histogram {Name}", name);
        }
    }

    public IDisposable StartTimer(string name, Dictionary<string, object>? tags = null)
    {
        var histogram = GetOrCreateHistogram($"{name}_duration", tags);

        if (tags != null && tags.Count > 0)
        {
            var labelValues = tags.Values.Select(v => v.ToString() ?? string.Empty).ToArray();
            return histogram.WithLabels(labelValues).NewTimer();
        }
        else
        {
            return histogram.NewTimer();
        }
    }

    public void RecordEvent(string name, string description, Dictionary<string, object>? tags = null)
    {
        // Prometheus doesn't have a direct event concept, so we increment a counter
        IncrementCounter($"event_{name}_total", 1, tags);
        _logger.LogInformation("Prometheus event recorded: {Name} - {Description}", name, description);
    }

    private Counter GetOrCreateCounter(string name, Dictionary<string, object>? tags)
    {
        var key = GenerateMetricKey(name, tags);
        if (!_counters.TryGetValue(key, out var counter))
        {
            var labelNames = tags?.Keys.ToArray() ?? Array.Empty<string>();
            counter = Prometheus.Metrics.CreateCounter(
                SanitizeName(name),
                $"Counter for {name}",
                labelNames);
            _counters[key] = counter;
        }

        return counter;
    }

    private Gauge GetOrCreateGauge(string name, Dictionary<string, object>? tags)
    {
        var key = GenerateMetricKey(name, tags);
        if (!_gauges.TryGetValue(key, out var gauge))
        {
            var labelNames = tags?.Keys.ToArray() ?? Array.Empty<string>();
            gauge = Prometheus.Metrics.CreateGauge(
                SanitizeName(name),
                $"Gauge for {name}",
                labelNames);
            _gauges[key] = gauge;
        }

        return gauge;
    }

    private Histogram GetOrCreateHistogram(string name, Dictionary<string, object>? tags)
    {
        var key = GenerateMetricKey(name, tags);
        if (!_histograms.TryGetValue(key, out var histogram))
        {
            var labelNames = tags?.Keys.ToArray() ?? Array.Empty<string>();

            // Choose appropriate buckets based on metric name
            var buckets = DetermineBuckets(name);

            histogram = Prometheus.Metrics.CreateHistogram(
                SanitizeName(name),
                $"Histogram for {name}",
                new HistogramConfiguration
                {
                    LabelNames = labelNames,
                    Buckets = buckets
                });
            _histograms[key] = histogram;
        }

        return histogram;
    }

    private static double[] DetermineBuckets(string metricName)
    {
        var lowerName = metricName.ToLowerInvariant();

        // Use size buckets for size-related metrics
        if (lowerName.Contains("size") || lowerName.Contains("bytes") || lowerName.Contains("length"))
        {
            return DefaultSizeBuckets;
        }

        // Use exponential buckets for count-related metrics
        if (lowerName.Contains("count") || lowerName.Contains("total"))
        {
            return Histogram.ExponentialBuckets(1, 2, 10); // 1, 2, 4, 8, 16, 32, 64, 128, 256, 512
        }

        // Default to latency buckets for duration/time metrics
        return DefaultLatencyBuckets;
    }

    private static string GenerateMetricKey(string name, Dictionary<string, object>? tags)
    {
        // Key is based on metric name and label names (not values)
        var sanitizedName = SanitizeName(name);
        if (tags == null || tags.Count == 0)
            return sanitizedName;

        var labelNames = string.Join("_", tags.Keys.OrderBy(k => k));
        return $"{sanitizedName}_{labelNames}";
    }

    private static string SanitizeName(string name)
    {
        // Prometheus metric names must match [a-zA-Z_:][a-zA-Z0-9_:]*
        return name.Replace(".", "_").Replace("-", "_").ToLowerInvariant();
    }
}
