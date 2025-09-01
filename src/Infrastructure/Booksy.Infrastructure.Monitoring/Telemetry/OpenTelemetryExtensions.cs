// ========================================
// Telemetry/OpenTelemetryExtensions.cs
// ========================================
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Booksy.Infrastructure.Monitoring.Telemetry;

/// <summary>
/// Extension methods for OpenTelemetry configuration
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing and metrics
    /// </summary>
    public static IServiceCollection AddOpenTelemetryMonitoring(
        this IServiceCollection services,
        string serviceName,
        string serviceVersion)
    {
        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) =>
                        {
                            // Don't trace health checks
                            return !httpContext.Request.Path.StartsWithSegments("/health");
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                    })
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.SetDbStatementForText = true;
                    })
                    .AddSource("MassTransit")
                    .AddSource("Booksy.*");

                // Add exporters based on configuration
                if (Environment.GetEnvironmentVariable("JAEGER_ENDPOINT") != null)
                {
                    tracing.AddJaegerExporter();
                }

                if (Environment.GetEnvironmentVariable("OTLP_ENDPOINT") != null)
                {
                    tracing.AddOtlpExporter();
                }

                // Always add console exporter in development
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("Booksy.*");

                // Add exporters
                if (Environment.GetEnvironmentVariable("PROMETHEUS_ENDPOINT") != null)
                {
                    metrics.AddPrometheusExporter();
                }

                if (Environment.GetEnvironmentVariable("OTLP_ENDPOINT") != null)
                {
                    metrics.AddOtlpExporter();
                }

                // Console exporter for development
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    metrics.AddConsoleExporter();
                }
            });

        return services;
    }

    /// <summary>
    /// Adds custom activity source for tracing
    /// </summary>
    public static IServiceCollection AddCustomActivitySource(
        this IServiceCollection services,
        string sourceName)
    {
        services.AddSingleton(new ActivitySource(sourceName));
        return services;
    }
}