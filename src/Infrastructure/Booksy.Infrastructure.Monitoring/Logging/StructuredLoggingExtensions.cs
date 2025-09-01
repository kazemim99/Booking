// ========================================
// Logging/StructuredLoggingExtensions.cs
// ========================================
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Compact;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Booksy.Infrastructure.Monitoring.Logging;

/// <summary>
/// Extension methods for structured logging configuration
/// </summary>
public static class StructuredLoggingExtensions
{
    /// <summary>
    /// Configures Serilog with structured logging
    /// </summary>
    public static LoggerConfiguration ConfigureStructuredLogging(
        this LoggerConfiguration loggerConfiguration,
        string applicationName,
        string environment)
    {
        return loggerConfiguration
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("ApplicationName", applicationName)
            .Enrich.WithProperty("Environment", environment)
            .Enrich.WithCorrelationId()
            .WriteTo.Console(new CompactJsonFormatter())
            .WriteTo.File(
                new CompactJsonFormatter(),
                $"logs/{applicationName}-.json",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30);
    }

    /// <summary>
    /// Adds correlation ID to log context
    /// </summary>
    public static void EnrichWithCorrelationId(this ILogger logger, string correlationId)
    {
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // The correlation ID will be included in all log entries within this scope
        }
    }

    /// <summary>
    /// Logs a performance metric
    /// </summary>
    public static void LogPerformance(
        this ILogger logger,
        string operation,
        long elapsedMilliseconds,
        Dictionary<string, object>? additionalProperties = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["ElapsedMilliseconds"] = elapsedMilliseconds,
            ["IsSlowOperation"] = elapsedMilliseconds > 1000
        };

        if (additionalProperties != null)
        {
            foreach (var prop in additionalProperties)
            {
                properties[prop.Key] = prop.Value;
            }
        }

        if (elapsedMilliseconds > 1000)
        {
            logger.LogWarning("Slow operation detected: {Operation} took {ElapsedMilliseconds}ms {@Properties}",
                operation, elapsedMilliseconds, properties);
        }
        else
        {
            logger.LogInformation("Operation completed: {Operation} in {ElapsedMilliseconds}ms {@Properties}",
                operation, elapsedMilliseconds, properties);
        }
    }

    /// <summary>
    /// Logs a business event
    /// </summary>
    public static void LogBusinessEvent(
        this ILogger logger,
        string eventName,
        string eventDescription,
        LogLevel level = LogLevel.Information,
        Dictionary<string, object>? properties = null)
    {
        var logProperties = new Dictionary<string, object>
        {
            ["EventName"] = eventName,
            ["EventDescription"] = eventDescription,
            ["EventTimestamp"] = DateTime.UtcNow
        };

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                logProperties[prop.Key] = prop.Value;
            }
        }

        logger.Log(level, "Business Event: {EventName} - {EventDescription} {@Properties}",
            eventName, eventDescription, logProperties);
    }
}