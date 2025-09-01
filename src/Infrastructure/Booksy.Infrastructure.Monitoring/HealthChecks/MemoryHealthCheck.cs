// ========================================
// HealthChecks/CustomHealthChecks.cs
// ========================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http;

namespace Booksy.Infrastructure.Monitoring.HealthChecks;

/// <summary>
/// Custom health check for memory usage
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _maximumMemoryMb;

    public MemoryHealthCheck(long maximumMemoryMb)
    {
        _maximumMemoryMb = maximumMemoryMb;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocated = GC.GetTotalMemory(false) / (1024 * 1024);
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);

        var data = new Dictionary<string, object>
        {
            ["AllocatedMB"] = allocated,
            ["Gen0Collections"] = gen0,
            ["Gen1Collections"] = gen1,
            ["Gen2Collections"] = gen2
        };

        if (allocated <= _maximumMemoryMb)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                $"Memory usage is normal: {allocated} MB",
                data));
        }

        if (allocated <= _maximumMemoryMb * 1.5)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Memory usage is high: {allocated} MB",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy(
            $"Memory usage is critical: {allocated} MB",
            data: data));
    }
}