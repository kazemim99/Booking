// ========================================
// Metrics/IMetricsCollector.cs
// ========================================
namespace Booksy.Infrastructure.Monitoring.Metrics;

/// <summary>
/// Interface for collecting application metrics
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Records a counter metric
    /// </summary>
    void IncrementCounter(string name, double value = 1, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records a gauge metric
    /// </summary>
    void RecordGauge(string name, double value, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records a histogram metric
    /// </summary>
    void RecordHistogram(string name, double value, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Starts a timer for measuring duration
    /// </summary>
    IDisposable StartTimer(string name, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records an event
    /// </summary>
    void RecordEvent(string name, string description, Dictionary<string, object>? tags = null);
}