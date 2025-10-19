// ========================================
// Booksy.API/Observability/SerilogConfiguration.cs
// Centralized Serilog configuration for all APIs
// ========================================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace Booksy.API.Observability;

public static class SerilogConfiguration
{
    public static void ConfigureSerilog(IConfiguration configuration, string applicationName)
    {
        var seqUrl = configuration["Observability:Seq:Url"] ?? "http://localhost:5342";
        var seqApiKey = configuration["Observability:Seq:ApiKey"];
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";

        Log.Logger = new LoggerConfiguration()
            // Minimum level
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)

            // Enrichers - Add contextual information to all log events
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.WithProperty("Environment", environment)
            .Enrich.WithExceptionDetails() // Detailed exception logging

            // Console sink - for development
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                restrictedToMinimumLevel: LogEventLevel.Debug)

            // File sink - rolling daily logs
            .WriteTo.File(
                path: $"logs/{applicationName}-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information)

            // File sink - Compact JSON format for parsing
            .WriteTo.File(
                new CompactJsonFormatter(),
                path: $"logs/{applicationName}-.json",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                restrictedToMinimumLevel: LogEventLevel.Information)

            // Seq sink - centralized logging
            .WriteTo.Seq(seqUrl, apiKey: seqApiKey)

            .CreateLogger();
    }

    public static IHostBuilder UseSerilogWithConfiguration(
        this IHostBuilder hostBuilder,
        string applicationName)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            var seqUrl = context.Configuration["Observability:Seq:Url"] ?? "http://localhost:5341";
            var seqApiKey = context.Configuration["Observability:Seq:ApiKey"];
            var environment = context.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";

            configuration
                // Minimum level
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)

                // Enrichers
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.WithProperty("Environment", environment)
                .Enrich.WithExceptionDetails()

                // Read configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)

                // Console sink
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                    restrictedToMinimumLevel: LogEventLevel.Debug)

                // File sink - rolling daily logs
                .WriteTo.File(
                    path: $"logs/{applicationName}-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Information)

                // File sink - Compact JSON
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    path: $"logs/{applicationName}-.json",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    restrictedToMinimumLevel: LogEventLevel.Information)

                // Seq sink
                .WriteTo.Seq(seqUrl, apiKey: seqApiKey);
        });
    }
}
