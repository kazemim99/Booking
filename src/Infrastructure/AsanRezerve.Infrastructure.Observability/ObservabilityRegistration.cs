using AsanRezerve.Infrastructure.Observability.Diagnostics;
using AsanRezerve.Infrastructure.Observability.Logging.Levels;
using AsanRezerve.Infrastructure.Observability.LogStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog.Core;

namespace AsanRezerve.Infrastructure.Observability;

/// <summary>The log store, diagnostics and admin API services. Logging itself is <c>AddAsanRezerveLogging</c>.</summary>
public static class ObservabilityRegistration
{
    public static IServiceCollection AddAsanRezerveObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(LogStoreOptions.Section);
        services.Configure<LogStoreOptions>(section);
        var options = section.Get<LogStoreOptions>() ?? new LogStoreOptions();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ApplicationCounters>();
        services.AddSingleton<CacheDiagnostics>();
        services.AddScoped<SystemOverviewService>();

        services.AddDbContext<ObservabilityDbContext>((sp, db) => db.UseNpgsql(
            sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection"),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ObservabilityDbContext.Schema)));

        services.AddSingleton(sp => new LogStoreDataSource(
            sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required by the log store.")));
        services.AddSingleton<LogPartitions>();
        services.AddScoped<LogQueryService>();

        if (options.Enabled)
        {
            services.AddSingleton<ILogEventBatchWriter, NpgsqlLogEventBatchWriter>();
            services.AddSingleton<LogStoreWriter>();
            services.AddHostedService(sp => sp.GetRequiredService<LogStoreWriter>());
            services.AddSingleton<ILogEventSink, LogStoreSink>();
            services.AddHostedService<LogPartitionMaintenanceService>();
            services.Replace(ServiceDescriptor.Singleton<ILogLevelOverrideStore, EfLogLevelOverrideStore>());
        }

        return services;
    }

    /// <summary>
    /// Applies the <c>observability</c> migration, creates the current days' log partitions, then lets the log store
    /// start writing. Never fails startup: the API must run without its log store, which then only buffers (and, when
    /// full, drops) events. Without its day partitions the store still works: events wait in the default partition
    /// and move into their day when the hourly maintenance creates it.
    /// </summary>
    public static async Task MigrateObservabilityStoreAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AsanRezerve.Observability.LogStore");
        try
        {
            await scope.ServiceProvider.GetRequiredService<ObservabilityDbContext>().Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The observability log store could not be migrated; stored logs are unavailable until the next start");
            return;
        }

        try
        {
            await services.GetRequiredService<LogPartitions>().EnsureAsync(services.GetRequiredService<TimeProvider>().GetUtcNow());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The log store's day partitions could not be created; events wait in the default partition");
        }

        services.GetService<LogStoreWriter>()?.MarkReady();
    }
}
