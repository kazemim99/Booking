using AsanRezerve.Infrastructure.Observability.Logging.Masking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Compact;

namespace AsanRezerve.Infrastructure.Observability.Logging;

/// <summary>
/// The host's logging: Microsoft.Extensions.Logging decides what is logged, Serilog where it goes (design D1).
/// <para>The host used <c>UseSerilog</c>, whose logger factory bypasses MEL's filters, and had no Serilog level
/// configuration — so every <c>Logging:LogLevel</c> value was ignored and production logged every EF Core SQL
/// command and every routing step at Information, synchronously. Registered as an ordinary provider, Serilog only
/// receives what <c>Logging:LogLevel</c> (and the runtime overrides) let through; <c>IsEnabled</c> is exact per
/// category, so a disabled Debug call costs a lookup, not a formatted message.</para>
/// </summary>
public static class LoggingPipeline
{
    public static ILoggingBuilder AddAsanRezerveLogging(this ILoggingBuilder logging, IConfiguration configuration)
    {
        logging.ClearProviders();
        logging.Services.Configure<ObservabilityLoggingOptions>(configuration.GetSection(ObservabilityLoggingOptions.Section));
        logging.Services.AddSingleton<ILoggerProvider>(sp =>
            new SerilogLoggerProvider(CreateLogger(sp, configuration), dispose: true));
        return logging;
    }

    /// <summary>
    /// Serilog's own minimum is Verbose: MEL has already filtered. Every event is masked before any sink, then
    /// fanned out to console and rolling file (both asynchronous), Seq when configured, and every
    /// <see cref="ILogEventSink"/> in DI (the database log store). A sink resolved here must not depend on
    /// <see cref="ILogger"/> — logging is what is being built.
    /// </summary>
    internal static Logger CreateLogger(IServiceProvider services, IConfiguration configuration)
    {
        var options = services.GetRequiredService<IOptions<ObservabilityLoggingOptions>>().Value;

        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.With(new SensitiveDataMaskingEnricher());

        if (options.Console.Enabled)
        {
            var template = options.Console.OutputTemplate;
            logger.WriteTo.Async(sink => sink.Console(outputTemplate: template), bufferSize: options.AsyncBufferSize);
        }

        if (options.File.Enabled)
        {
            var file = options.File;
            logger.WriteTo.Async(sink => sink.File(
                    new CompactJsonFormatter(),
                    file.Path,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: (long)Math.Max(1, file.FileSizeLimitMb) * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: Math.Max(1, file.RetainedFileCount)),
                bufferSize: options.AsyncBufferSize);
        }

        var seqUrl = string.IsNullOrWhiteSpace(options.Seq.ServerUrl) ? configuration["Seq:ServerUrl"] : options.Seq.ServerUrl;
        if (!string.IsNullOrWhiteSpace(seqUrl))
        {
            logger.WriteTo.Seq(seqUrl, apiKey: options.Seq.ApiKey, queueSizeLimit: options.Seq.QueueSizeLimit);
        }

        foreach (var sink in services.GetServices<ILogEventSink>())
        {
            logger.WriteTo.Sink(sink);
        }

        return logger.CreateLogger();
    }
}
