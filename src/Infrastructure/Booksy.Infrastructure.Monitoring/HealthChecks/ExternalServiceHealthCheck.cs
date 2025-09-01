// ========================================
// HealthChecks/ExternalServiceHealthCheck.cs
// ========================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Monitoring.HealthChecks;

/// <summary>
/// Custom health check for external services
/// </summary>
public class ExternalServiceHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceName;
    private readonly string _healthEndpoint;
    private readonly ILogger<ExternalServiceHealthCheck> _logger;

    public ExternalServiceHealthCheck(
        HttpClient httpClient,
        string serviceName,
        string healthEndpoint,
        ILogger<ExternalServiceHealthCheck> logger)
    {
        _httpClient = httpClient;
        _serviceName = serviceName;
        _healthEndpoint = healthEndpoint;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(_healthEndpoint, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy($"{_serviceName} is healthy");
            }

            return HealthCheckResult.Unhealthy(
                $"{_serviceName} returned status code {response.StatusCode}",
                data: new Dictionary<string, object>
                {
                    ["StatusCode"] = response.StatusCode,
                    ["Endpoint"] = _healthEndpoint
                });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Health check failed for {ServiceName}", _serviceName);

            return HealthCheckResult.Unhealthy(
                $"Failed to connect to {_serviceName}",
                ex,
                new Dictionary<string, object>
                {
                    ["Endpoint"] = _healthEndpoint,
                    ["Error"] = ex.Message
                });
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                $"Health check timed out for {_serviceName}",
                data: new Dictionary<string, object>
                {
                    ["Endpoint"] = _healthEndpoint,
                    ["Timeout"] = _httpClient.Timeout.TotalSeconds
                });
        }
    }
}
