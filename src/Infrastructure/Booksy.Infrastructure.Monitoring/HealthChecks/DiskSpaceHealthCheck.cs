// ========================================
// HealthChecks/DiskSpaceHealthCheck.cs
// ========================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Monitoring.HealthChecks;

/// <summary>
/// Custom health check for disk space
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly long _minimumFreeMegabytes;
    private readonly ILogger<DiskSpaceHealthCheck> _logger;

    public DiskSpaceHealthCheck(
        long minimumFreeMegabytes,
        ILogger<DiskSpaceHealthCheck> logger)
    {
        _minimumFreeMegabytes = minimumFreeMegabytes;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\");
            var freeSpaceMb = driveInfo.AvailableFreeSpace / (1024 * 1024);

            if (freeSpaceMb >= _minimumFreeMegabytes)
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Disk space is sufficient: {freeSpaceMb:N0} MB free",
                    new Dictionary<string, object>
                    {
                        ["FreeSpaceMB"] = freeSpaceMb,
                        ["TotalSpaceMB"] = driveInfo.TotalSize / (1024 * 1024),
                        ["Drive"] = driveInfo.Name
                    }));
            }

            if (freeSpaceMb >= _minimumFreeMegabytes / 2)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Disk space is low: {freeSpaceMb:N0} MB free",
                    data: new Dictionary<string, object>
                    {
                        ["FreeSpaceMB"] = freeSpaceMb,
                        ["MinimumRequiredMB"] = _minimumFreeMegabytes,
                        ["Drive"] = driveInfo.Name
                    }));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Disk space is critically low: {freeSpaceMb:N0} MB free",
                data: new Dictionary<string, object>
                {
                    ["FreeSpaceMB"] = freeSpaceMb,
                    ["MinimumRequiredMB"] = _minimumFreeMegabytes,
                    ["Drive"] = driveInfo.Name
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check disk space");

            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Failed to check disk space",
                ex));
        }
    }
}
