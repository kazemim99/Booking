namespace Booksy.Core.Domain.Application.Services
{
    /// <summary>
    /// Metrics collection service
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Increments a counter metric
        /// </summary>
        /// <param name="counterName">Counter name</param>
        /// <param name="tags">Optional tags</param>
        void IncrementCounter(string counterName, Dictionary<string, string>? tags = null);

        void RecordHistogram(string name, double value);

        /// <summary>
        /// Records a metric value
        /// </summary>
        /// <param name="metricName">Metric name</param>
        /// <param name="value">Metric value</param>
        /// <param name="tags">Optional tags</param>
        void RecordValue(string metricName, double value, Dictionary<string, string>? tags = null);
    }
}