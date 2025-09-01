// ========================================
// DependencyInjection/MonitoringExtensions.cs
// ========================================
using Booksy.Infrastructure.Monitoring.HealthChecks;
using Booksy.Infrastructure.Monitoring.Metrics;
using Booksy.Infrastructure.Monitoring.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using System.Net.Http;
using System.Text.Json;

namespace Booksy.Infrastructure.Monitoring;

/// <summary>
/// Extension methods for monitoring configuration
/// </summary>
public static class MonitoringExtensions
{
    /// <summary>
    /// Adds monitoring services
    /// </summary>
    public static IServiceCollection AddMonitoring(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add metrics collector
        var metricsProvider = configuration["Monitoring:Metrics:Provider"] ?? "Default";
        if (metricsProvider == "Prometheus")
        {
            services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();
        }
        else
        {
            services.AddSingleton<IMetricsCollector, MetricsCollector>();
        }

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<MemoryHealthCheck>(
                "memory",
                tags: new[] { "ready" })
            .AddCheck<DiskSpaceHealthCheck>(
                "disk",
                tags: new[] { "ready" });

        // Add OpenTelemetry if configured
        if (configuration.GetValue<bool>("Monitoring:OpenTelemetry:Enabled"))
        {
            var serviceName = configuration["Monitoring:OpenTelemetry:ServiceName"] ?? "Booksy";
            var serviceVersion = configuration["Monitoring:OpenTelemetry:ServiceVersion"] ?? "1.0.0";
            services.AddOpenTelemetryMonitoring(serviceName, serviceVersion);
        }

        return services;
    }

    /// <summary>
    /// Uses monitoring middleware
    /// </summary>
    public static IApplicationBuilder UseMonitoring(this IApplicationBuilder app)
    {
        // Use Prometheus metrics endpoint
        app.UseHttpMetrics();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapMetrics("/metrics");
        });

        // Use health checks with custom response
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        });

        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse
        });

        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteHealthCheckResponse
        });

        return app;
    }

    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                exception = e.Value.Exception?.Message,
                data = e.Value.Data
            })
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}